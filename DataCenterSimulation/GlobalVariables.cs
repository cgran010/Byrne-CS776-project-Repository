using System;
using System.Data;
using System.IO;
using System.Text;

namespace DataCenterSimulation
{
	// This class started out as a Global Variables class, thus the name GV.
	// It evolved to also include some common routines.
	public class GV
    {
		// 8 hours in seconds
		public const int shiftLength = 28800;
		// New Shifts start one hour apart
		// one hour in seconds
		public const int oneHour = 3600;
		//one hour in timespan
		public static TimeSpan shiftOffset = new TimeSpan(0, 1, 0, 0);	



		//Total number of spaces in the parking garage
		public const int totalSpaces = 2560;
		//Number of employees in each shift
		public const int shiftSize = 320;
		//Input data size for all jobs in Mb
		public const int inputDataSize = 4000;
		// Size of a virtual machine in M=b
		public const int VMSize = 4000;


		public const int numRegions = 4;
		public const int numGroupsperRegion = 4;
		public const int numAPsperGroup = 4;
		public const int numSpacesperAP = 40;
		//Destination address for the Data Center
		public const int DCAddress = 2560;

		// The starting time/date of the simulation is unimportant, so I chose 1/1/2020
		public static DateTime currentTime = new DateTime(2020, 1, 1, 0, 0, 0);

		//Counts the number of seconds since the simulation was begun.
		public static int intElapsedTime = 0;

		// The possible states of the VM on a given vehicle. 
		// available: the car is available to be assigned a job
		// Transmitting: The car is transmitting data
		// Receiving:  The car is receiving data
		// Processing:  The car is running a job:
		// Idle:  The car is available to accept a backup
		// Holding:  The car is holding a backup
		// Holding for return: The car is waiting to transfer the VM back to the new vehicle in the previous space.
		// Unavailable:  The car will be leaving soon and is unavailable for use.
		public enum VMStatus {
			justArrived,		// 0
			processing,			// 1
			transmitting,		// 2
			receiving,          // 3
			idle,				// 4
			holding,            // 5
			holdingForReturn,   // 6
			unavailable         // 7
		}

		// The possible transmission types.  I use these to determine what state
		// to return a vehicle to after the transmission is complete
		//
		public enum TransmissionType
		{
			recInputData,			// Receiving the initial input data for a job
			recVM,                  // Receiving the VM from a departing vehicle
			recBackupData,			// Receiving backup data from the working vehicle
			recReturnVM,			// Receiving the VM back from the backup vehicle -- used to transfer the job back to the 
									// new vehicle occupying the spot where the working vehicle had been
			transFinalData,			// transmitting the final data back to the data center
			transVM,				// transmitting the VM at the end of shift
			transReturnVM,			// transmitting the VM back to the new vehicle
			transBackups,			// transmitting backups to our backup vehicles
			transBackupCopy,		// transmitting backup from a departing backup vehicle to one taking it's place
			transLocal,				// transmitting the local backup
			transRandom,			// transmitting the random backup
			none					// not receiving or transmitting
		}

		// Possible destinations for transmissions
		public enum BackupsNeeded
		{
			BothBackups, LocalBackup, RandomBackup, none
		}


		// Each generated job has an integer job number which is its unique identifier.
		public static int currentJobNumber = 0;

		// If a car becomes idle too close to the end of its shift, it will become 
		// unavailable and refuse to accept new jobs.

		public const int tooClose = 3600;
		
		// The output csv file.  This file will contain the data needed for 
		// analysis.  For each job completed, a record will be added which
		// includes the job number, the expected run time, the start and end
		// times, and the elapsed time in seconds.
		public const string OutputFileName = "ByrneSimOutput.csv";
		public static StringBuilder csv = new StringBuilder();

		public static DataTable GlobalComms = new DataTable();

		// Determine the region number of a given space
		public static int FindRegionNumberfromSpace(int space)
		{
			return space / (GV.numSpacesperAP * GV.numAPsperGroup * GV.numGroupsperRegion);
		}       // Determine the region number of a given space

		// Determine the Group number of a given space
		public static int FindGroupNumberfromSpace(int space)
		{
			int Group = space % (GV.numSpacesperAP * GV.numAPsperGroup * GV.numGroupsperRegion);
			Group = Group / (GV.numSpacesperAP * GV.numAPsperGroup);
			return Group;
		}

		// Determine the Group number of a given space
		public static int FindAPNumberfromSpace(int space)
		{
			int AP = space % (GV.numSpacesperAP * GV.numAPsperGroup);
			AP = AP / (GV.numSpacesperAP);
			return AP;
		}

		// Determine the Lot number whithin a cluster of a given space
		public static int FindLotNumberfromSpace(int space)
		{
			return space % (GV.numSpacesperAP);
		}

		// Create the global communications array datatable.  

		//private static Random rand = new Random();

		//public static int GenerateRandomNumer(int Lower,int Upper)
		//{

		//	int Number = rand.Next(Lower, Upper);
			StringBuilder csv2 = new StringBuilder();

		//	csv2.AppendLine(Number.ToString());
		//	File.AppendAllText("rands.txt", csv2.ToString());
		//	return Number;
		//}

		//Function to get a random number 
		private static readonly Random random = new Random();
		private static readonly object syncLock = new object();
		public static int GenerateRandomNumer(int min, int max)
		{
			int Number = 0;
			lock (syncLock)
			{ // synchronize
				Number = random.Next(min, max);
			}
			//StringBuilder csv2 = new StringBuilder();

			//	csv2.AppendLine(Number.ToString());
			//	File.AppendAllText("rands.txt", csv2.ToString());
			return Number;
		}


	} // class GV
} // namespace DataCenterSimulation
