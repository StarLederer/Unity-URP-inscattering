using System.Collections.Generic;
using UnityEngine;

namespace HL.Inscattering
{
	public class InscatteringVolumeManager
	{
		private static List<InscatteringVolume> m_InscatteringVolumes;
		public static List<InscatteringVolume> InscatteringVolumes
		{
			get
			{
				if (m_InscatteringVolumes == null)
				{
					m_InscatteringVolumes = new List<InscatteringVolume>();
				}


				return m_InscatteringVolumes;
			}
		}
	}
}