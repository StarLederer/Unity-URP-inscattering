using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HL.URPInscattering
{
	[ExecuteAlways]
	[RequireComponent(typeof(Light))]
	public class AutomaticInscatteringVolume : InscatteringVolume
	{
		Light m_Light;

		private void Awake()
		{
			m_Light = GetComponent<Light>();
		}

		public override Color GetColor()
		{
			if (!m_Light)
				Awake();

			return m_Light.color * m_Light.intensity;
		}

		private void Update()
		{
			if (!m_Light)
				Awake();

			transform.localScale = new Vector3(m_Light.range, m_Light.range, m_Light.range);
		}
	}
}
