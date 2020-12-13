using System;
using System.Data;

namespace DataCenterSimulation
{
	/*
	 * The GroupHub class encapsulates a single group hub.  It is of type node.
	 */
	public class GroupHub : Node
	{
		AccessPoint[] AccessPoints = new AccessPoint[GV.numAPsperGroup];
		int numUploadstoRegion = 0;

		public GroupHub(int node)
		{
			DownSpeed = 4000;     // Mbps
			UpSpeed = 4000;     // Mbps
			numTransmissionRequests = 0;
			downloadRequest = false;
			NodeNum = node;
			for (int i = 0; i < GV.numAPsperGroup; i++)
			{
				AccessPoints[i] = new AccessPoint(NodeNum * GV.numAPsperGroup + i, this);
			}
		} // GroupHub

		public bool InitializeSpace(int space, bool SimStart)
		{
			//int AP = space % (GV.numSpacesperAP * GV.numAPsperGroup);
			//AP = AP / (GV.numSpacesperAP);
			int AP = GV.FindAPNumberfromSpace(space);
			return AccessPoints[AP].InitializeSpace(space, SimStart);
		}

		public void InitializeSpaces()
		{
			for (int i = 0; i < GV.numAPsperGroup; i++)
				AccessPoints[i].InitializeSpaces();
		}

		// This call is used at shift change to initialize spaces where new cars have arrived.
		// Cars are always started at idle so that they will be available to accept new jobs.
		public void InitializeSpacesOnShiftChange()
		{
			for (int i= 0;i<AccessPoints.Length;i++)
			AccessPoints[i].InitializeSpacesOnShiftChange();
		}


		public override void RequestTransmissionSlot(int destinationSpace)
		{
			numTransmissionRequests++;

			if (GV.FindGroupNumberfromSpace(destinationSpace) != NodeNum)
				numUploadstoRegion++;
		}  
		
		// AddTransmissionRequest
		// The transmit subcycle gathers all transmission requests, both trans
		// mitting and receiving. The purpose of this subcycle is to  be able to 
		// determine the transmit speed allocated to each transmission.
		public void Transmit()
		{
			for (int i = 0; i < AccessPoints.Length; i++)
			{
				AccessPoints[i].Transmit();
			}
		}  // Transmit


		// The receive cycle returns back to each transmitter how much data they were actually 
		// able to transmit.  It might be lower than they expected because of a bottleneck on 
		// the receiving end.
		public void Receive()
		{
			for (int i = 0; i < AccessPoints.Length; i++)
			{
				AccessPoints[i].Receive();
			}
		}  // Receive

		// This function will assign an existing job to a new vehicle.
		// At this point in time it is used only for when a vehicle is backing
		// up a new job to other cars.
		public void AssignJobtoVehicle(Job job, int DestinationSpace, int WorkingVehicle, int dataSize)
		{
			int AP = GV.FindAPNumberfromSpace(DestinationSpace);
			AccessPoints[AP].AssignJobtoVehicle(job, DestinationSpace, WorkingVehicle, dataSize);
		}

		// When a car finishes it's job, it will call this function to release the backup
		// vehicles
		public void DeleteBackup(int SpaceNumber)
		{
			int AP = GV.FindAPNumberfromSpace(SpaceNumber);
			AccessPoints[AP].DeleteBackup(SpaceNumber);
		}

		// Sends a message to the backup machine to notify it that it will be
		// receiving the VM corresponding to its job.
		public Boolean NotifyPartnerofTransfer(int SpaceNumber, double DataSize, GV.TransmissionType transType, Job job)
		{
			int AP = GV.FindAPNumberfromSpace(SpaceNumber);
			return AccessPoints[AP].NotifyPartnerofTransfer(SpaceNumber, DataSize, transType, job);
		}

	} // class GroupHub

} // namespace DataCenterSimulation
