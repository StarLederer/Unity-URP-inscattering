using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HL.URPInscattering
{
	[ExecuteAlways]
	public abstract class InscatteringVolume : MonoBehaviour
	{
		private void OnEnable()
		{
			InscatteringVolumeManager.InscatteringVolumes.Add(this);
		}

		private void OnDisable()
		{
			InscatteringVolumeManager.InscatteringVolumes.Remove(this);
		}

		private void Update()
		{
			transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x, transform.localScale.x);
		}

		public abstract Color GetColor();
	}
}
