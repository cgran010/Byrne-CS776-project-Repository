using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataCenterSimulation
{
	public partial class DataCenter : Form
	{

		// Constants representing the indices into the parkingLot Data Table
		const int colRegion = 0;
		const int colGroup = 1;
		const int colAP = 2;
		const int colAPSlot = 3;
		const int colParkingSpace = 4;
		const int colOccupied = 5;
		const int colStatus = 6;
		const int colArrTime = 7;
		const int colDepTime = 8;
		const int colJobSize = 9;
		const int colOutputDataSize = 10;


		/*
         * parkingLot always contains the current state of the parking lot.  Information it
         * includes is:
         *  Region number
         *  Group number
         *  AP number
         *  Space Number within cluster
         *  Whether or not the spot is occupied
         *  The time the current vehicle arrived
         *  The time the current vehicle leaves.
         *  
         *  I will also probably have to add a field or fields relating to the job status of the
         *  vehicle:
         *  Job Status: available or busy
         *  time left in job
         */
		static DataTable parkingLot = new DataTable();

		static RegionHub[] Regions = new RegionHub[GV.numRegions];

		public DataCenter()
		{
			InitializeComponent();
		}


		// On load, we will initialize the data.
		private void frmDataCenter_Load(object sender, EventArgs e)
		{
			lblStartTime.Text = GV.currentTime.ToString();

			// Create the four Regions
			for (int i = 0; i < GV.numRegions; i++)
			{
				Regions[i] = new RegionHub(i);
			}

			// Create a randomized array of parking space numbers.  This will be used to set the remaining 
			// shift time for each car, so that we don't have all 40 cars in a cluster or all 160 in a group
			// changing shifts at once.
			int[] RandArray = new int[GV.totalSpaces];
			CreateRandomizedArray(RandArray);

			// Set the remaining for each car in the lot.  There are 320 cars in each shift, so after every 320
			// cars we deduct an hour from the time remaining.  We are filling to just before a shift change, 
			// so the max time remaining is 7 hours, and the last batch of cars is ready to leave.
			//
			// Also, attempt to assign a job to the car.  The job will be accepted if the car is in a available state and 
			// has more than 0 time left.


			CreateGlobalCommunicationsTable();

			//StringBuilder csv2 = new StringBuilder();
			//csv2.Clear();
			//csv2.AppendLine("Space Number,Status,Partner,Speed");
			//for (int i = 0; i < GV.totalSpaces; i++)
			//{
			//	csv2.AppendLine(i
			//		+ "," + (GV.VMStatus)GV.GlobalComms.Rows[i]["Status"]
			//		+ "," + GV.GlobalComms.Rows[i]["Partner"].ToString()
			//		+ "," + GV.GlobalComms.Rows[i]["Speed"].ToString());
			//}
			//File.WriteAllText("initialGC.csv", csv2.ToString());

			// For the initial parking fill only, we do not actually worry about
			// transmitting the job, becausae we assume that was done before the 
			// simulation started.
			int timeRemaining = GV.shiftLength;
			for (int i = 0; i < GV.totalSpaces; i++)
			{
				GV.GlobalComms.Rows[RandArray[i]]["Time"] = timeRemaining;
				if ((i + 1) % 320 == 0)
					timeRemaining -= GV.oneHour;
			}
			for (int i = 0; i < GV.numRegions; i++)
			{
				Regions[i].InitializeSpaces();
			}
			//csv2.Clear();
			//csv2.AppendLine("Space Number,Status,Partner,Speed");
			//for (int i = 0; i < GV.totalSpaces; i++)
			//{
			//	csv2.AppendLine(i
			//		+ "," + (GV.VMStatus)GV.GlobalComms.Rows[i]["Status"]
			//		+ "," + GV.GlobalComms.Rows[i]["Partner"].ToString()
			//		+ "," + GV.GlobalComms.Rows[i]["Speed"].ToString());
			//}
			//File.WriteAllText("initialGC.csv", csv2.ToString());
			var filepath = GV.OutputFileName;
			//if (File.Exists(filepath))
			//{
			//	File.Delete(filepath);
			//}
			try
			{
				using (StreamWriter writer = new StreamWriter(new FileStream(filepath,
				FileMode.Create, FileAccess.Write)))
				{
					writer.WriteLine("jobNumber,StartTime,EndTime,JobLength,ElapsedTime");
				}
			}
			catch (UnauthorizedAccessException ex)
			{
				MessageBox.Show(ex.Message + Environment.NewLine +
								"There was an error attempting to write to the output file.  " +
								"Please make sure that you do not have the file open in another program.",
								"Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.Close();
			}

		} // frmDataCenter_Load


		//  Create an array with all the parking space numbers 0 - 2559 in a randomized order.  
		//  This is used at load time to do the initial fill of the parking lot.
		static void CreateRandomizedArray(int[] array)
		{
			for (int space = 0; space < array.Length; space++)
			{
				array[space] = space;
			}
			Shuffle(array);
		} // CreateRandomizedArray



		/*
         * Shuffle randomizes a list of integers.  It is used to determine where the next car will park.
         * 
         * Found at: https://thedeveloperblog.com/fisher-yates-shuffle
         */
		static void Shuffle(int[] array)
		{
			var rand = new Random();
			int n = array.Length;
			for (int i = 0; i < n; i++)
			{
				// NextDouble returns a random number between 0 and 1.
				int r = i + (int)(rand.NextDouble() * (n - i));
				int t = array[r];
				array[r] = array[i];
				array[i] = t;
			}
		} // End Shuffle


		private CancellationTokenSource _canceller;

		private async void btnStart_Click(object sender, EventArgs e)
		{
			GV.intElapsedTime = 0;
			btnStart.Enabled = false;
			btnStop.Enabled = true;
			TimeSpan oneSecond = new TimeSpan(0, 0, 0, 1);

			_canceller = new CancellationTokenSource();
			await Task.Run(() =>
			{
				// Main time loop
				do
				{
					//if (GV.intElapsedTime == 0)
					//{
						if (lblElapsedTime.InvokeRequired)
							lblElapsedTime.Invoke(new Action(() => lblElapsedTime.Text = GV.currentTime.ToString()));
						else
							lblElapsedTime.Text = GV.currentTime.ToString();
					//}

					//every hour on the hour, we need to do a shift change
					if (GV.intElapsedTime % GV.oneHour == 0)
						ShiftChange();



					// Gather all transmission requests
					Transmit();

					//Set the transmission speed at each vehicle to be the lower of the 
					//speeds at source and destination.  The Data Center is a special case and 
					//is always considered to have 1Gb communication speed.
					GV.GlobalComms.Rows[GV.DCAddress]["Speed"] = 1000;
					for (int SourceIndex = 0; SourceIndex < GV.GlobalComms.Rows.Count; SourceIndex++)
					{
						int DestIndex = (int)GV.GlobalComms.Rows[SourceIndex]["Partner"];
						 
						if (DestIndex != -1 && Convert.ToDouble(GV.GlobalComms.Rows[SourceIndex]["Speed"]) >
								Convert.ToDouble(GV.GlobalComms.Rows[DestIndex]["Speed"]))
						{
							GV.GlobalComms.Rows[SourceIndex]["Speed"]
								= GV.GlobalComms.Rows[DestIndex]["Speed"];

							if ((int)GV.GlobalComms.Rows[DestIndex]["Partner"] != SourceIndex)
							{
								GV.GlobalComms.Rows[DestIndex]["Partner"] = SourceIndex;
								GV.GlobalComms.Rows[DestIndex]["Status"] = GV.VMStatus.receiving;
							}


						}

					}

					// The end of the transmit/receive cycle where every vehicle requesting a
					// transmission finds out how many Mbs it was actually able to transmit.
					Receive();

					//StartCycle();
					//EndCycle();

					//		//foreach (DataRow row in parkingLot.Rows)
					//		for (int i = 0; i < GV.totalSpaces; i++)
					//			{
					//			if (DateTime.Compare(parkingLot.Rows[i].Field<DateTime>("departureTime"), GV.currentTime) == 0)
					//			{
					//				//MessageBox.Show("Spot " + row[colParkingSpace].ToString() + " is ready to leave.");
					//				InitializeSpace(i);
					//			}
					//		};

					GV.intElapsedTime++;
					GV.currentTime += oneSecond;

					//Check to see if the user has stopped the simulation.
					if (_canceller.Token.IsCancellationRequested)
						break;

				} while (true);
			}); // await task run

			_canceller.Dispose();
			btnStart.Enabled = true;
			btnStop.Enabled = false;
		} // btnStart_Click

		// The transmit subcycle gathers all transmission requests, both trans
		// mitting and receiving. The purpose of this subcycle is to  be able to 
		// determine the transmit speed allocated to each transmission.
		private void Transmit()
		{
			for (int i = 0; i < Regions.Length; i++)
			{
				Regions[i].Transmit();
			}
		}  // Transmit

		// The receive cycle returns back to each transmitter how much data they were actually 
		// able to transmit.  It might be lower than they expected because of a bottleneck on 
		// the receiving end.
		private void Receive()
		{
			GV.csv.Clear();

			for (int i = 0; i < Regions.Length; i++)
			{
				Regions[i].Receive();
			}
			if (GV.csv.Length > 0)
				File.AppendAllText(GV.OutputFileName, GV.csv.ToString());
		}  // Receive


		private void ShiftChange()
		{
			for (int i = 0; i < Regions.Length; i++)
			{
				Regions[i].InitializeSpacesOnShiftChange();
			}
		}

		private void btnStop_Click(object sender, EventArgs e)
		{
			_canceller.Cancel();
		} // btnStop_Click

		// Creates the global communications table used to shedule transmissions
		static void CreateGlobalCommunicationsTable()
		{
			GV.GlobalComms.Clear();
			DataColumn colMySpace = new DataColumn("MySpace");
			colMySpace.DataType = System.Type.GetType("System.Int32");
			GV.GlobalComms.Columns.Add(colMySpace);

			DataColumn colPartner = new DataColumn("Partner");
			colPartner.DataType = System.Type.GetType("System.Int32");
			GV.GlobalComms.Columns.Add(colPartner);

			DataColumn colPrimary = new DataColumn("Primary");
			colPrimary.DataType = System.Type.GetType("System.Int32");
			GV.GlobalComms.Columns.Add(colPrimary);

			DataColumn colLocal = new DataColumn("LocalBackup");
			colLocal.DataType = System.Type.GetType("System.Int32");
			GV.GlobalComms.Columns.Add(colLocal);

			DataColumn colRandom = new DataColumn("RandomBackup");
			colRandom.DataType = System.Type.GetType("System.Int32");
			GV.GlobalComms.Columns.Add(colRandom);

			DataColumn colStatus = new DataColumn("Status");
			colStatus.DataType = System.Type.GetType("System.Int32");
			GV.GlobalComms.Columns.Add(colStatus);

			DataColumn colSpeed = new DataColumn("Speed");
			colSpeed.DataType = System.Type.GetType("System.Double");
			GV.GlobalComms.Columns.Add(colSpeed);

			DataColumn colTime = new DataColumn("Time");
			colTime.DataType = System.Type.GetType("System.Int32");
			GV.GlobalComms.Columns.Add(colTime);

			for (int i = 0; i <= GV.totalSpaces; i++)
			{
				DataRow newSpace = GV.GlobalComms.NewRow();
				newSpace["MySpace"] = i;
				newSpace["Speed"] = 0.0;
				newSpace["Partner"] = i;
				newSpace["Status"] = GV.VMStatus.idle;
				newSpace["Time"] = 0;
				newSpace["Primary"] = -1;
				newSpace["LocalBackup"] = -1;
				newSpace["RandomBackup"] = -1;
				GV.GlobalComms.Rows.Add(newSpace);
			}

		} // CreateGlobalCommunicationsTable

		// This function will find a local and random backup spot for a car
		// at the primary location passed in.
		// primary: the parking space of the car needing backups.
		// Local:  the space number of an idle car in the same cluster.
		// Random:  the space number of an idle car anywhere in the garage.
		// Returns true if the backup spaces are found, false otherwise.
		public static Boolean FindBackupLocations(int SpaceNumber)
		{
			Boolean bFound = false;

			// Find a local spot
			if (FindLocalBackupLocation(SpaceNumber))
			{

				// if no Random backup space was found, release the local space.
				if (!FindRandomBackupLocation(SpaceNumber))
				{
					GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"]]["Status"] = GV.VMStatus.idle;
					GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"]]["Primary"] = -1;
					GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"]]["LocalBackup"] = -1;
					GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"] = -1;
					GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.idle;
				} // if (!bRandom)
				else
				{
					GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"]]["RandomBackup"]
						= GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"];
					GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"]]["LocalBackup"]
						= GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"];
					bFound = true;
				} // if bLocal
			}
			return bFound;
		} // FindBackupLocations

		// This function will find a local backup spot for a car
		// at the primary location passed in.
		// primary: the parking space of the car needing backups.
		// Local:  the space number of an idle car in the same cluster.
		// Returns true if the backup space is found, false otherwise.
		public static Boolean FindLocalBackupLocation(int SpaceNumber)
		{
			int ClusterMin = (SpaceNumber / 40) * 40;
			int ClusterMax = ClusterMin + GV.numSpacesperAP - 1;
			Boolean bLocal = false;
			int i = ClusterMin;

			// Find a local spot.ac
			do
			{
				if ((GV.VMStatus)GV.GlobalComms.Rows[i]["Status"] == GV.VMStatus.idle  
					&& i != SpaceNumber && (int)GV.GlobalComms.Rows[i]["Time"] > GV.tooClose
					&& Math.Abs((int)GV.GlobalComms.Rows[i]["Time"]
						- (int)GV.GlobalComms.Rows[SpaceNumber]["Time"]) > GV.oneHour * 2
					&& ((int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"] == -1
					|| (int)GV.GlobalComms.Rows[i]["Time"] 
						!= (int)GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"]]["Time"]))

				{
					GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"] = i;
					GV.GlobalComms.Rows[i]["Primary"] = SpaceNumber;
					GV.GlobalComms.Rows[i]["LocalBackup"] = i;
					GV.GlobalComms.Rows[i]["Status"] = GV.VMStatus.holding;
					bLocal = true;

				} // if status is idle
				i++;
			} while (!bLocal && i < ClusterMax);
			return bLocal;
		}  // FindLocalBackupLocation

		// This function will find a random backup spot for a car
		// at the primary location passed in.
		// Random:  the space number of an idle car anywhere in the garage.
		// Returns true if the backup spaces are found, false otherwise.
		public static Boolean FindRandomBackupLocation(int SpaceNumber)
		{
			Boolean bRandom = false;
			int j = 0;

			int[] RandArray = new int[GV.totalSpaces];
			CreateRandomizedArray(RandArray);
			do
			{
				int k = RandArray[j];
				// We want to make sure that the backup car and the working car are not 
				// leaving at or near the same time.
				if ((GV.VMStatus) GV.GlobalComms.Rows[k]["Status"] == GV.VMStatus.idle
					&& k != SpaceNumber
					&& Math.Abs((int) GV.GlobalComms.Rows[k]["Time"]
					- (int)GV.GlobalComms.Rows[SpaceNumber]["Time"]) > GV.oneHour * 2
					&& (int)GV.GlobalComms.Rows[k]["Time"] > GV.tooClose
					&& ((int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"] == -1
						|| (int)GV.GlobalComms.Rows[k]["Time"]
						!= (int)GV.GlobalComms.Rows[(int)GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"]]["Time"]))
				{

					GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"] = k;
					GV.GlobalComms.Rows[k]["Primary"] = SpaceNumber;
					GV.GlobalComms.Rows[k]["RandomBackup"] = k;
					GV.GlobalComms.Rows[k]["Status"] = GV.VMStatus.holding;
					bRandom = true;
				}
				j++;
			} while (!bRandom && j<GV.totalSpaces);
			return bRandom;
		} //FindRandomBackupLocation

		// This function will find a local and random backup spot for a car
		// at the primary location passed in.
		// primary: the parking space of the car needing backups.
		// Local:  the space number of an idle car in the same cluster.
		// Random:  the space number of an idle car anywhere in the garage.
		// Returns true if the backup spaces are found, false otherwise.
		public static Boolean FindMigrationPartner(int Space, ref int Partner)
		{

			int ClusterMin = (Space / 40) * 40;
			int ClusterMax = ClusterMin + GV.numSpacesperAP - 1;
			Boolean bFound = false;
			int i = ClusterMin;
			int j = 0;

			// Find a local spot.ac
			do
			{
				if ((GV.VMStatus)GV.GlobalComms.Rows[i]["Status"] == GV.VMStatus.idle)
				{
					Partner = i;
					bFound = true;
					GV.GlobalComms.Rows[i]["Status"] = GV.VMStatus.holding;
				} // if status is idle
				i++;
			} while (!bFound && i < ClusterMax);

			// find a random spot
			if (!bFound)
			{
				int[] RandArray = new int[GV.totalSpaces];
				CreateRandomizedArray(RandArray);
				do
				{
					if ((GV.VMStatus)GV.GlobalComms.Rows[RandArray[j]]["Status"] == GV.VMStatus.idle)

					{
						Partner = RandArray[j];
						GV.GlobalComms.Rows[RandArray[j]]["Status"] = GV.VMStatus.holding;
					}
					j++;
				} while (!bFound && j < GV.totalSpaces);

			}// Find Random

			return (bFound);
		} // FindMigrationPartner

		// This function will assign an existing job to a new vehicle.
		// At this point in time it is used only for when a vehicle is backing
		// up a new job to other cars.
		public static void AssignJobtoVehicle(Job job, int DestinationSpace, int WorkingVehicle, int dataSize)
		{
			int Region = GV.FindRegionNumberfromSpace(DestinationSpace);
			Regions[Region].AssignJobtoVehicle(job, DestinationSpace, WorkingVehicle, dataSize);
		}

		// When a car finishes it's job, it will call this function to release the backup
		// vehicles
		public static void DeleteBackup(int Backup1, int Backup2)
		{
			int Region = 0;

			if (Backup1 != -1)
			{
				Region = GV.FindRegionNumberfromSpace(Backup1);
				Regions[Region].DeleteBackup(Backup1);
			}
			if (Backup2 != -1)
			{
				Region = GV.FindRegionNumberfromSpace(Backup2);
				Regions[Region].DeleteBackup(Backup2);
			}
		}

		// Sends a message to the backup machine to notify it that it will be
		// receiving the VM corresponding to its job.
		public static Boolean NotifyPartnerofTransfer(int SpaceNumber, double DataSize, GV.TransmissionType transType, Job job)
		{
			int Region = GV.FindRegionNumberfromSpace(SpaceNumber);
			return Regions[Region].NotifyPartnerofTransfer(SpaceNumber, DataSize, transType, job);
		}

		public static Boolean CheckForExistingJob(int SpaceNumber)
		{
			Boolean bFound = false;

			for (int i = 0; i < GV.totalSpaces; i++)
			{
				if ((GV.VMStatus)GV.GlobalComms.Rows[i]["Status"] == GV.VMStatus.transmitting &&
					(int)GV.GlobalComms.Rows[i]["Partner"] == SpaceNumber)
				{
					GV.GlobalComms.Rows[SpaceNumber]["Primary"] = (int)GV.GlobalComms.Rows[i]["Primary"];
					GV.GlobalComms.Rows[SpaceNumber]["LocalBackup"] = (int)GV.GlobalComms.Rows[i]["LocalBackup"];
					GV.GlobalComms.Rows[SpaceNumber]["RandomBackup"] = (int)GV.GlobalComms.Rows[i]["RandomBackup"];
					GV.GlobalComms.Rows[SpaceNumber]["Status"] = GV.VMStatus.receiving;
					return true;
				}
			}

			return false;
		}

		public static void AssignJobtoBackups(Job job, int PrimarySpace)
		{
			Job Backup1Job = new Job(job);
			Job Backup2Job = new Job(job);

			int backupdatasize = job.inputDataSize;
			int Backup1Space = Convert.ToInt32(GV.GlobalComms.Rows[PrimarySpace]["LocalBackup"]);
			int Backup2Space = Convert.ToInt32(GV.GlobalComms.Rows[PrimarySpace]["RandomBackup"]);

			AssignJobtoVehicle(Backup1Job, Backup1Space, PrimarySpace, backupdatasize);
			AssignJobtoVehicle(Backup2Job, Backup2Space, PrimarySpace, backupdatasize);

		}


	}  // frmDataCenter


} // DataCenterSimulation
