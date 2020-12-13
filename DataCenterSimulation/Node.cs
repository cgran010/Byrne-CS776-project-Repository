using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenterSimulation
{
	/* A node is either a hub or an access point.  It does not store or 
	 * process data, merely transmits it up or down.  Therefore, it only 
	 * needs to keep track of how many requests it has currently, and what
	 * its transmission speeds are.
	 */
	 public abstract class Node

	{
		public int NodeNum = 0;			// each node keeps track of its own number.
		public int DownSpeed = 0;       // Mbps
		public int UpSpeed = 0;			// Mbps
		public int numTransmissionRequests = 0;
		public bool downloadRequest = false;

		//Adds an upload transmission request 
		public abstract void RequestTransmissionSlot(int destination);


		// Set the downLoadRequest flag to true
		public void setDownloadRequest()
		{
			downloadRequest = true;
		} //setDownloadRequest

		// Returns the number of Mb transmitted in the current cycle.  This is 
		// calculated by dividing the APSpeed of 54Mbps by the number of 
		// transmission requests.  
		public double GetMbTransmittedDown()
		{
			int returnVal = numTransmissionRequests;
			if (downloadRequest)
				returnVal++;
			returnVal = returnVal / DownSpeed;
			return returnVal;
		} // GetMbTransmitted

		// Returns the number of Mb transmitted up to the next node.  
		public double GetMbTransmittedUp()
		{
				return numTransmissionRequests * DownSpeed;

		} // GetMbTransmittedUp


	} // class node
} //namespace DataCenterSimulation
