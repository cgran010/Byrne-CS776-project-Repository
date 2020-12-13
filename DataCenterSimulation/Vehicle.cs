using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataCenterSimulation
{
	class Vehicle
	{
//		public GV.VMStatus Status = GV.VMStatus.idle; //Processing, transmitting, receiving or idle
		public Job CurrentJob = null;    //The current running job.  0 if there is no current job
		private readonly List<Job> BackupJobs = new List<Job>();

		// So that the vehicle can communicate with its access point
		private AccessPoint LocalAP;
		private int DownloadDataSize = 0;
		public int SpaceNumber = 0;    // parking space number 0-2559 in the lot
		//private readonly DateTime ShiftEnd;
		//public int TimeRemaininginShift = GV.shiftLength;  // the length of an 8 hour shift in seconds
		private GV.TransmissionType TransType = GV.TransmissionType.none;
		private GV.BackupsNeeded BackupsNeeded = GV.BackupsNeeded.none;

		// An estimate of the time it will take to Transfer the Final Data to the DC.
		private int TimeToTransferFinalData;
		// An estimate of the time it will take to transfer the VM and intermediate data to another machine
		private int TimeToTransferVM;
		// When to start the migration.
		private int StartMigration;

		// Variables to keep track of job locations and backups.  If a car is running a job, then its 
		// own space number will be in Working Copy.  Otherwise, it will be one of the two backup
//		private int PrimaryBackup = -1;
//		private int SecondBackup = -1;
//		private int WorkingCopy = -1;


		//When a new job is started, this is set to the input data size.  It can also be set 
		//when this vehicle is reciving a backup from another car.
		private double RemainingDatatoTransmit = 0;


		//private TransmissionPacket uploadPacket = null;
		//private TransmissionPacket downloadPacket = null;


		public Vehicle( int space, AccessPoint AP)
		{
			LocalAP = AP;
//			GV.GlobalComms.Rows[space]["Status"] = GV.VMStatus.justArrived;
		}

		/*
		 * Accepts a new job from the data center.  Returns true if the job was accepted, 
		 * false if it was not.
		 */
		public Boolean AcceptJob( bool SimStart)
		{
			Boolean jobAccepted = false;

			if (!SimStart && (GV.VMStatus)GV.GlobalComms.Rows[SpaceNumber]["Status"] == GV.VMStatus.justArrived)
			{
				if (DataCenter.CheckForExistingJob(SpaceNumber))
				{
					TransType = GV.TransmissionType.recReturnVM;
					return true;
				}
				else
					GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.idle;
			}
			if ((int)GV.GlobalComms.Rows[SpaceNumber]["Time"] <= GV.tooClose)
			{
				if (SimStart || (GV.VMStatus)GV.GlobalComms.Rows[SpaceNumber]["Status"] == GV.VMStatus.idle)
					GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.unavailable;
			}
			else if (((GV.VMStatus)GV.GlobalComms.Rows[SpaceNumber]["Status"] == GV.VMStatus.idle)
				|| SimStart)
			{

				if ((GV.VMStatus)GV.GlobalComms.Rows[SpaceNumber]["Status"] == GV.VMStatus.idle)
				{
					if (DataCenter.FindBackupLocations(SpaceNumber))
					{

						GV.GlobalComms.Rows[SpaceNumber]["Primary"] = SpaceNumber;

						Job NewJob = new Job();

						CurrentJob = NewJob;

						DataCenter.AssignJobtoBackups(CurrentJob, SpaceNumber);
						if (SimStart)
						{
							GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.processing;
							BackupsNeeded = GV.BackupsNeeded.none;
							GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
						}
						else
						{
							GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.receiving;
							TransType = GV.TransmissionType.recInputData;
							BackupsNeeded = GV.BackupsNeeded.BothBackups;
							DownloadDataSize = CurrentJob.inputDataSize;
							RemainingDatatoTransmit = CurrentJob.inputDataSize;
							GV.GlobalComms.Rows[SpaceNumber]["Partner"] = GV.DCAddress;
							// To do, set backup locations currentjobs to new job
						}

						// Estimate of how long it will take to offload final data
						TimeToTransferFinalData = (int)Math.Round(CurrentJob.outputDataSize / (54.0 / 20), 0);
						TimeToTransferVM = TimeToTransferFinalData +
							(int)Math.Round(4000 / (54.0 / 20), 0);
						jobAccepted = true;
					} // find backup locations successful
					else
					{
						//MessageBox.Show("W:" + SpaceNumber + "No backup found");
						GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.idle;
						GV.GlobalComms.Rows[SpaceNumber]["Primary"] = -1;
					}
				}  // if available
				else // not available
				{
					//do nothing
				}

			}


			return jobAccepted;
			
		} // AcceptJob


		// AddTransmissionRequest
		// The transmit subcycle gathers all transmission requests, both trans
		// mitting and receiving. The purpose of this subcycle is to  be able to 
		// determine the transmit speed allocated to each transmission.
		//
		// Returns 1 if there is a transmission request, 0 if not.
		public int Transmit()
		{

			int ReturnVal = 0;

			switch ((GV.VMStatus)GV.GlobalComms.Rows[SpaceNumber]["Status"])
			{


				case GV.VMStatus.transmitting:
				case GV.VMStatus.receiving:
					ReturnVal = 1;
					break;
				default:
					GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
					break;

			}  // switch (Status)
			return ReturnVal;
		}  // Transmit

		// The receive cycle is at the end of the time loop.  It checks the status of each car and takes appropriate 
		// action based on the status and the transmission type, if any.
		public void Receive()
		{
			// Decrement the time left in the shift for th ecurrent car.
			GV.GlobalComms.Rows[SpaceNumber]["Time"] = (int)GV.GlobalComms.Rows[SpaceNumber]["Time"] - 1;

			// Check the car's status
			switch ((GV.VMStatus)GV.GlobalComms.Rows[SpaceNumber]["Status"])
			{
				// If the car was processing, then call to run the current job.  
				case GV.VMStatus.processing:
					// If the current job is complete, set the status to transmitting, and the transmission
					// type to final data. 
					if (CurrentJob.Run())    // returns true if job is complete
					{
						GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.transmitting;
						TransType = GV.TransmissionType.transFinalData;
						RemainingDatatoTransmit = CurrentJob.outputDataSize;
						GV.GlobalComms.Rows[SpaceNumber]["Partner"] = GV.DCAddress;
					}
					// If the job didn't complete, then check to see if we are leaving soon and need to start
					// migrating our VM.  If so, we always migrate to the car holding our random backup.  That 
					// car will send the job back to the new car occupying this same spot.
					else if ((int)GV.GlobalComms.Rows[SpaceNumber]["Time"] <= StartMigration)
					{
						GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.transmitting;
						TransType = GV.TransmissionType.transVM;
						int randomBackup = Convert.ToInt32(GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"]);
						if (randomBackup != -1)
						{
							GV.GlobalComms.Rows[SpaceNumber]["Partner"] = randomBackup;
							RemainingDatatoTransmit = GV.VMSize + CurrentJob.outputDataSize;
							DataCenter.NotifyPartnerofTransfer((int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"], RemainingDatatoTransmit, GV.TransmissionType.recVM, CurrentJob);
						}
					}
					break;
					//If we are holding, we first check to see if we need to offload the backup.
					//We do that if we have less than an hour in our shift.
				case GV.VMStatus.holding:
					int newBackupLoc = -1;
					if ((int)GV.GlobalComms.Rows[SpaceNumber]["Time"] <= GV.tooClose)
					{
						Boolean bBackupFound = false;
						int PrimaryCar = (int)GV.GlobalComms.Rows[SpaceNumber]["Primary"];
						if (Convert.ToInt32(GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"]) == SpaceNumber
							&& Convert.ToInt32(GV.GlobalComms.Rows[PrimaryCar]["LocalBackup"]) != -1)
						{
							bBackupFound = DataCenter.FindRandomBackupLocation(PrimaryCar);
							if (bBackupFound)
							{
								newBackupLoc = (int)GV.GlobalComms.Rows[PrimaryCar]["RandomBackup"];
								// Update the two backup locations so that they know about each other.
								int localBackup = (int)GV.GlobalComms.Rows[PrimaryCar]["LocalBackup"];
								GV.GlobalComms.Rows[newBackupLoc]["LocalBackup"] = localBackup;
								GV.GlobalComms.Rows[localBackup]["RandomBackup"] = newBackupLoc;
							}
						}
						else
						{
							if (Convert.ToInt32(GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"]) == SpaceNumber
							&& Convert.ToInt32(GV.GlobalComms.Rows[PrimaryCar]["RandomBackup"]) != -1)
							{
								bBackupFound = DataCenter.FindLocalBackupLocation(PrimaryCar);
							}

							if (bBackupFound)
							{
								newBackupLoc = (int)GV.GlobalComms.Rows[PrimaryCar]["LocalBackup"];
								// Update the two backup locations so that they know about each other.
								int randomBackup = (int)GV.GlobalComms.Rows[PrimaryCar]["RandomBackup"];
								GV.GlobalComms.Rows[newBackupLoc]["RandomBackup"] = randomBackup;
								GV.GlobalComms.Rows[randomBackup]["LocalBackup"] = newBackupLoc;
							}
						}

						if (bBackupFound)
						{
							int DataSize = CurrentJob.inputDataSize;
							DataCenter.NotifyPartnerofTransfer(newBackupLoc, CurrentJob.inputDataSize,
								GV.TransmissionType.recBackupData, CurrentJob);
							// Let the new backup vehicle know what's up.
							DataCenter.AssignJobtoVehicle(CurrentJob, newBackupLoc, PrimaryCar, DataSize);
							GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.transmitting;
							GV.GlobalComms.Rows[SpaceNumber]["Partner"] = newBackupLoc;
							GV.GlobalComms.Rows[newBackupLoc]["Partner"] = SpaceNumber;
							TransType = GV.TransmissionType.transBackupCopy;
							
							RemainingDatatoTransmit = DataSize;
						}
					}
					break;
				// Holding for return is a special status meaning that the primary car running our job has decided to 
				// leave and has passed us their VM and intermediate data.  We have to hold onto the job until the new
				// car arrives at the start of the next shift.  When that happens, we change our status to transmitting
				// and notify the new car that it will be getting the VM back so that it can resume the job our predecessor
				// began.
				case GV.VMStatus.holdingForReturn:
					// Wait until the top of the next hour to send the VM back to the new car
					if (GV.intElapsedTime % GV.oneHour == 0)
					{
						GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.transmitting;
						TransType = GV.TransmissionType.transReturnVM;
						RemainingDatatoTransmit = GV.VMSize + CurrentJob.outputDataSize;
						DataCenter.NotifyPartnerofTransfer(Convert.ToInt32(GV.GlobalComms.Rows[SpaceNumber]["Primary"]), RemainingDatatoTransmit, 
							GV.TransmissionType.recReturnVM, CurrentJob);
					}
					break;

				// If we are idle or unavailable, we do nothing.  The unavailable status will be changed when a new car
				// arrives in the spot.  The idle status will change when we accept a job or a backup.
				case GV.VMStatus.idle:
				case GV.VMStatus.unavailable:
					break;

				// There are many transmission types, and each has diffent actions associated with it.
				default:		// transmitting or receiving

					// decrement the remaining data count by the effective speed of transmission.
					RemainingDatatoTransmit -= Convert.ToDouble(GV.GlobalComms.Rows[SpaceNumber]["Speed"]);

					// If we have finished transmitting:
					if (RemainingDatatoTransmit <= 0)
					{
						switch (TransType)
						{
							//Done receiving input data, need to make 2 backups.
							case GV.TransmissionType.recInputData:
								GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.transmitting;
								TransType = GV.TransmissionType.transBackups;
								BackupsNeeded = GV.BackupsNeeded.BothBackups;
								RemainingDatatoTransmit = GV.inputDataSize;
								if ((int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"] == -1)
								{
									MessageBox.Show("Primary Backup Vehicle invalid", "Fatal Error",
										MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
								}
								else
								{
									GV.GlobalComms.Rows[SpaceNumber]["Partner"] = GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"];
									GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"]]["Partner"] = SpaceNumber;
								}
								DataCenter.NotifyPartnerofTransfer((int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"], 
									RemainingDatatoTransmit, GV.TransmissionType.recBackupData, CurrentJob);

								break;
							// Check to see if both backups are complete.  If not, start the second backup.
							case GV.TransmissionType.transBackups:
								if (BackupsNeeded == GV.BackupsNeeded.BothBackups)
								{
									BackupsNeeded = GV.BackupsNeeded.RandomBackup;
									RemainingDatatoTransmit = GV.inputDataSize;
									if ((int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"] == -1)
									{
										MessageBox.Show("Secondary Backup Vehicle invalid", "Fatal Error",
											MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
									}
									else
									{
										GV.GlobalComms.Rows[SpaceNumber]["Partner"] = Convert.ToInt32(GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"]);
										GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"]]["Partner"] = SpaceNumber;
									}
									DataCenter.NotifyPartnerofTransfer((int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"], RemainingDatatoTransmit, GV.TransmissionType.recBackupData, CurrentJob);
								}
								else if (BackupsNeeded == GV.BackupsNeeded.LocalBackup || BackupsNeeded == GV.BackupsNeeded.RandomBackup)
								{
									if ((int)GV.GlobalComms.Rows[SpaceNumber]["Time"] > TimeToTransferFinalData + CurrentJob.jobLength)
										StartMigration = 0;
									else
										StartMigration = TimeToTransferVM;
									GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.processing;
									GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
									TransType = GV.TransmissionType.none;
									BackupsNeeded = GV.BackupsNeeded.none;
								}
								else // (BackupsNeeded == GV.BackupsNeeded.none
									MessageBox.Show("Transmitting backups when none needed.", "Fatal Error",
													MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
								break;

							// Ive offloaded my backup to another vehicle, and am waiting to leave.  Set my
							// status to unavailable.
							case GV.TransmissionType.transBackupCopy:
								GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.unavailable;
								GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
								break;
							// I'm done receiving backup data, set my status to holding
							case GV.TransmissionType.recBackupData:
								GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.holding;
								GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
								TransType = GV.TransmissionType.none;
								break;
							// I received the backup from a departing vehicle, now I have to hold it until
							// the new vehicle arrives.
							case GV.TransmissionType.recVM:
								if (Convert.ToInt32(GV.GlobalComms.Rows[SpaceNumber]["Primary"]) == -1)
								{
									//MessageBox.Show("Can't receive VM.  Primary is -1");
								}
								else
								{
									GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.holdingForReturn;
									TransType = GV.TransmissionType.none;
									RemainingDatatoTransmit = GV.VMSize + CurrentJob.outputDataSize;
								}
								GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;

								break;
							// I am the new car and have received a VM from a backup car.  I start
							// running the program.
							case GV.TransmissionType.recReturnVM:
								GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.processing;
								GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
								break;
							// I am a departing car and have finished transmitting my VM.  I become unavailable.
							case GV.TransmissionType.transVM:
								GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.unavailable;
								GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;

								break;
							// I completed my job!  If it's too close to time to leave, I become unavailable.
							// Otherwise I set myself idle so that I can be assigned a new job or can become
							// a backup for another car.
							case GV.TransmissionType.transFinalData:
								if ((int)GV.GlobalComms.Rows[SpaceNumber]["Time"] <= GV.tooClose)
								{
									GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.unavailable;
									GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
								}
								else
								{
									GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.idle;
									GV.GlobalComms.Rows[SpaceNumber]["Partner"] = SpaceNumber;
								}

								// After the job is complete, write out the data to the .csv file.  The 
								// actual write to file takes place in the data center once per seond for
								// all cars.  This is to help the speed of the simulation.
								string OutputString = CurrentJob.jobNumber + ","
									+ CurrentJob.StartTime.ToString() + ","
									+ GV.currentTime.ToString() + ","
									+ CurrentJob.GetJobLength().ToString() + ","
									+ (GV.currentTime - CurrentJob.StartTime).TotalSeconds + ","
									+ SpaceNumber;

								GV.csv.AppendLine(OutputString);
								DataCenter.DeleteBackup((int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"], (int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"]);
								GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"] = -1;
								GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"] = -1;
								GV.GlobalComms.Rows[SpaceNumber]["Primary"] = -1;
								// set stop time
								break;
							default:
								break;
						}
						break;
					} // switch on transmission type.
					break;
			} // switch on VM status

		}  // Receive

		public void SetBackupAssignment(Job job, int dataSize, int WorkingVehicle)
		{
			CurrentJob = job;
			RemainingDatatoTransmit = dataSize;
			TransType = GV.TransmissionType.recBackupData;
			GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.holding;
			GV.GlobalComms.Rows[SpaceNumber]["Primary"] = WorkingVehicle;
			//PrimaryBackup = Backup1;
			//SecondBackup = Backup2;
		} // SetBackupAssignment

		public void DeleteBackup()
		{
			CurrentJob = null;
			RemainingDatatoTransmit = 0;
			TransType = GV.TransmissionType.none;
			GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.idle;
			GV.GlobalComms.Rows[SpaceNumber]["Primary"] = -1;

		} // DeleteBackup

		public Boolean  ReceiveNotificationofTransfer(double DataSize, GV.TransmissionType transType, Job job)
		{
			GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.receiving;
			TransType = transType;
			if (TransType == GV.TransmissionType.recVM || TransType == GV.TransmissionType.recReturnVM)
			{
				if (Convert.ToInt32(GV.GlobalComms.Rows[SpaceNumber]["Primary"]) == -1)
					return false;
				CurrentJob = job;
			}

			RemainingDatatoTransmit = DataSize;
			return true;
		}

	} // class Vehicle
} // namespace DataCenterSimulation
