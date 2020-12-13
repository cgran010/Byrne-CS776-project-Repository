using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenterSimulation
{
	public class Job
	{
		public readonly int inputDataSize = 4000;		//Mb

		public int jobNumber;				// a unique integer identifier
		public int jobLength;				// time in seconds that it takes to run
		public int investedRuntime;		// current invested time
		public DateTime StartTime;			// Time and Date job was created
		public int outputDataSize;			// Size of the intermediate and output data in Mb
											// All jobs have a constant input size of 4000Mb.  Defined in the GV class. 

		/* 
		 * When a job is constructed, it generates itself and Initializes all the member variables.
		 */
		public Job()
		{
			jobNumber = GetNewJobNumber();

			// Our slowest transfer speed is in Mbps, so when I generate a job, I calculate 
			// the job length in seconds, and the output data size in Mb.  Transmission time
			// of this metadata is negligible.
			jobLength = GV.GenerateRandomNumer(10800, 86400);

			outputDataSize = GV.GenerateRandomNumer(250, 1000) * 8;

			// Set the start time to the current simulation time.
			StartTime = GV.currentTime;
			investedRuntime = 0;

		} // Initialize

		//  This constructor makes a copy of an existing job. 
		public Job (Job OrigJob)
		{
			jobNumber = OrigJob.jobNumber;
			StartTime = OrigJob.StartTime;
			investedRuntime = OrigJob.investedRuntime;
			jobLength = OrigJob.jobLength;
			outputDataSize = OrigJob.outputDataSize;
		}

		// Increment the current job number and return the new number
		private int GetNewJobNumber()
		{
			return ++GV.currentJobNumber;
		} // GetNewJobNumber

		// Returns true if the job has been completed, false otherwise
		public Boolean JobComplete()
		{
			return (jobLength == investedRuntime);
		} // JobComplete
		
		/* Increments the invested runtime by one clock tick and returns
		 * true if the job is completed false if it is not.
		 */
		public Boolean Run()
		{
			investedRuntime++;
			return (jobLength == investedRuntime);
		} // Run

		public int GetJobLength()
		{
			return jobLength;
		}

	} // class Job
} // namespace DataCenterSimulation
