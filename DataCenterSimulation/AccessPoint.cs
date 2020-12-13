using System;
using System.Data;
using System.IO;

namespace DataCenterSimulation
{
	/*
	 * The AccessPoint class encapsulates a single access point.  The access point
	 * does nothing but transmit data up and down.  It has no storage or memory, so 
	 * every clock cycle, it forgets everything from the clock cycle before, and
	 * and each vehicle plus the Group Controller must request transmission again.
	 */
	public class AccessPoint : Node
	{
		Vehicle[] Vehicles = new Vehicle[GV.numSpacesperAP];

		// So that the Access point can communicate with its group
		private GroupHub LocalGroup;
		int numRequests = 0;

		// Initialize the AP object
		public AccessPoint(int node, GroupHub group)
		{
			DownSpeed = 54;		// Mbps
			UpSpeed = 4000;     // Mbps
			numTransmissionRequests = 0;
			downloadRequest = false;
			NodeNum = node;
			LocalGroup = group;

			//When the access point is first created, it will fill up it's lot with vehicles.  5 of the vehicles are left
			//idle for VM migration.
			for (int i = 0; i < GV.numSpacesperAP; i++)
			{
				Vehicles[i] = new Vehicle(NodeNum * 40 + i, this);
			}
		} // AccessPoint

		public bool InitializeSpace(int GlobalSpaceNumber, bool SimStart)
		{
			//int SpaceNum = space % (GV.numSpacesperAP);
			int LotSpaceNum = GV.FindLotNumberfromSpace(GlobalSpaceNumber);
			Vehicles[LotSpaceNum].SpaceNumber = GlobalSpaceNumber;
		return Vehicles[LotSpaceNum].AcceptJob(SimStart);
		}

		public void InitializeSpaces()
		{
			for (int i = 0; i < GV.numSpacesperAP; i++)
			{
				Vehicles[i].SpaceNumber = NodeNum * GV.numSpacesperAP + i;
				Vehicles[i].AcceptJob(true);
			}
		}

		// This call is used at shift change to initialize spaces where new cars have arrived.
		// Cars are always started at idle so that they will be available to accept new jobs.
		public void InitializeSpacesOnShiftChange()
		{
			for (int i = 0; i < Vehicles.Length; i++)
			{
				int SpaceNum = NodeNum * GV.numSpacesperAP + i;

				if ((int)GV.GlobalComms.Rows[SpaceNum]["Time"] == 0)
				{
					GV.GlobalComms.Rows[SpaceNum]["Time"] = GV.shiftLength;
					GV.GlobalComms.Rows[SpaceNum]["Status"] = GV.VMStatus.justArrived;
					GV.GlobalComms.Rows[SpaceNum]["Primary"] = -1;
					GV.GlobalComms.Rows[SpaceNum]["LocalBackup"] = -1;
					GV.GlobalComms.Rows[SpaceNum]["RandomBackup"] = -1;

					Vehicles[i].AcceptJob(false);
				}
			}

		}

		public void MbsUploaded(ref TransmissionPacket uploadPacket)
		{

			//uploadPacket.MbsTransmitted = (DownSpeed * 1.0) / numTransmissionRequests;
			//if (GV.FindAPNumberfromSpace(uploadPacket.destinationSpace) != NodeNum)
			//	LocalGroup.MbsTransmitted(uploadPacket);
		}

		// This counts up the total number of upload and download transmission 
		// requests.  It is called at the start of the cycle so that the AP
		// can figure out how many Mbs to allocate to each request.
		public override void RequestTransmissionSlot(int destinationSpace)
		{
			numTransmissionRequests++;

		}  // AddTransmissionRequest

		// AddTransmissionRequest
		// The transmit subcycle gathers all transmission requests, both trans
		// mitting and receiving. The purpose of this subcycle is to  be able to 
		// determine the transmit speed allocated to each transmission.
		public void Transmit()
		{
			// count the number of vehicles who have a transmit request.
			numRequests = 0;
			for (int i = 0; i < Vehicles.Length; i++)
			{
				numRequests += Vehicles[i].Transmit();
			}

			// Set the effective speed for each transmission based on the number
			// of transmit requests.  If the vehicle is not transmitting, the effective
			// speed will be 0.
			double EffectiveSpeed = 0;
			if (numRequests != 0)
				EffectiveSpeed = (DownSpeed * 1.0) / numRequests;
			for (int i = 0; i < Vehicles.Length; i++)
				switch ((GV.VMStatus)GV.GlobalComms.Rows[Vehicles[i].SpaceNumber]["Status"])
				{
					case GV.VMStatus.receiving:
					case GV.VMStatus.transmitting:
						GV.GlobalComms.Rows[i + NodeNum * GV.numSpacesperAP]["Speed"] = EffectiveSpeed;
						break;
					case GV.VMStatus.idle:
						Vehicles[i].AcceptJob(false);
						break;
					default:
						GV.GlobalComms.Rows[i + NodeNum * GV.numSpacesperAP]["Speed"] = 0;
						break;
				}


		}  // Transmit


		// The receive cycle returns back to each transmitter how much data they were actually 
		// able to transmit.  It might be lower than they expected because of a bottleneck on 
		// the receiving end.
		public void Receive()
		{
			for (int i = 0; i < Vehicles.Length; i++)
			{
				Vehicles[i].Receive();
			}

		}  // Receive

		// This function will assign an existing job to a new vehicle.
		// At this point in time it is used only for when a vehicle is backing
		// up a new job to other cars.
		public void AssignJobtoVehicle(Job job, int DestinationSpace, int WorkingVehicle, int dataSize)
		{
			int vehicle = GV.FindLotNumberfromSpace(DestinationSpace);
			Vehicles[vehicle].SetBackupAssignment(job, dataSize, WorkingVehicle);
		}

		// When a car finishes it's job, it will call this function to release the backup
		// vehicles
		public void DeleteBackup(int SpaceNumber)
		{
				int vehicle = GV.FindLotNumberfromSpace(SpaceNumber);
				Vehicles[vehicle].DeleteBackup();
		}

		// Sends a message to the backup machine to notify it that it will be
		// receiving the VM corresponding to its job.
		public Boolean NotifyPartnerofTransfer(int SpaceNumber, double DataSize, GV.TransmissionType transType, Job job)
		{
			if (SpaceNumber == -1)
				return false;
			else { 
			int vehicle = GV.FindLotNumberfromSpace(SpaceNumber);
			return Vehicles[vehicle].ReceiveNotificationofTransfer(DataSize, transType, job);
			}
		}

	} // class AccessPoint

} // namespace DataCenterSimulation
