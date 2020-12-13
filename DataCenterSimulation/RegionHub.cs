using System;
using System.Data;

namespace DataCenterSimulation
{
	/*
	 * The RegionHub class encapsulates a single group hub.  It is of type node.
	 */
	public class RegionHub : Node
	{
		GroupHub[] Groups = new GroupHub[GV.numGroupsperRegion];
		private int numUploadstoDC = 0;

		public RegionHub(int node)
		{
			DownSpeed = 4000;     // Mbps
			UpSpeed = 4000;     // Mbps
			numTransmissionRequests = 0;
			downloadRequest = false;
			NodeNum = node;
			for (int i = 0; i < GV.numGroupsperRegion; i++)
			{
				Groups[i] = new GroupHub(NodeNum * GV.numGroupsperRegion + i);
			}
		} // RegionHub

		public bool InitializeSpace(int space, bool SimStart)
		{
			//int Group = space % (GV.numSpacesperAP * GV.numAPsperGroup * GV.numGroupsperRegion);
			//Group = Group / (GV.numSpacesperAP * GV.numAPsperGroup);
			int Group = GV.FindGroupNumberfromSpace(space);
			return Groups[Group].InitializeSpace(space, SimStart);
		}

		public void InitializeSpaces()
		{
			for (int i = 0; i < GV.numGroupsperRegion; i++)
				Groups[i].InitializeSpaces();
		}

		// This call is used at shift change to initialize spaces where new cars have arrived.
		// Cars are always started at idle so that they will be available to accept new jobs.
		public void InitializeSpacesOnShiftChange()
		{
			for(int i = 0; i< Groups.Length; i++)
				Groups[i].InitializeSpacesOnShiftChange();
		}

		//public void StartCycle()
		//{
		//	numUploadstoDC = 0;
		//	for (int i = 0; i < Groups.Length; i++)
		//	{
		//		Groups[i].StartCycle();
		//	}
		//} // StartCycle

		//public void EndCycle()
		//{
		//	for (int i = 0; i < Groups.Length; i++)
		//	{
		//		Groups[i].EndCycle();
		//	}
		//} // EndCycle

		public override void RequestTransmissionSlot(int destinationSpace)
		{
			numTransmissionRequests++;

			if (GV.FindRegionNumberfromSpace(destinationSpace) != NodeNum)
				numUploadstoDC++;
		}  // AddTransmissionRequest


		//// Initialize the AP object
		//public override void Initialize()
		//{
		//	DownSpeed = 4000;     // Mbps
		//	UpSpeed = 4000;     // Mbps
		//	numTransmissionRequests = 0;
		//	downloadRequest = false;
		//} // Initialize

		// The transmit subcycle gathers all transmission requests, both trans
		// mitting and receiving. The purpose of this subcycle is to  be able to 
		// determine the transmit speed allocated to each transmission.
		public void Transmit()
		{
			for (int i = 0; i < Groups.Length; i++)
			{
				Groups[i].Transmit();
			}
		}  // Transmit

		// The receive cycle returns back to each transmitter how much data they were actually 
		// able to transmit.  It might be lower than they expected because of a bottleneck on 
		// the receiving end.
		public void Receive()
		{
			for (int i = 0; i < Groups.Length; i++)
			{
				Groups[i].Receive();
			}
		}  // Receive

		// This function will assign an existing job to a new vehicle.
		// At this point in time it is used only for when a vehicle is backing
		// up a new job to other cars.
		public void AssignJobtoVehicle(Job job, int DestinationSpace, int WorkingVehicle, int dataSize)
		{
			int Group = GV.FindGroupNumberfromSpace(DestinationSpace);
			Groups[Group].AssignJobtoVehicle(job, DestinationSpace, WorkingVehicle, dataSize);
		}

		// When a car finishes it's job, it will call this function to release the backup
		// vehicles
		public void DeleteBackup(int SpaceNumber)
		{
			int Group = GV.FindGroupNumberfromSpace(SpaceNumber);
			Groups[Group].DeleteBackup(SpaceNumber);
		}

		// Sends a message to the backup machine to notify it that it will be
		// receiving the VM corresponding to its job.
		public Boolean NotifyPartnerofTransfer(int SpaceNumber, double DataSize, GV.TransmissionType transType, Job job)
		{
			int Group = GV.FindGroupNumberfromSpace(SpaceNumber);
			return Groups[Group].NotifyPartnerofTransfer(SpaceNumber, DataSize, transType, job);
		}

	} // class RegionHub

} // namespace DataCenterSimulation
