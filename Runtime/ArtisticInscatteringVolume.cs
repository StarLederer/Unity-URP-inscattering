using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HL.URPInscattering
{
	[ExecuteAlways]
	public class ArtisticInscatteringVolume : InscatteringVolume
	{
		[ColorUsage(true, true)]
		public Color inscatteringColor;

		public override Color GetColor()
		{
			return inscatteringColor;
		}
	}
}
