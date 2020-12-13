using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenterSimulation
{
	public class TransmissionPacket
	{
		public int sourceSpace;      // 0 - 2559, 2560 if source is the DC
		public int destinationSpace; // 0 - 2559, 2560 if destination is the DC

		public double MbsTransmitted;

	} // class TransmissionPacket
} // namespace DataCenterSimulation
