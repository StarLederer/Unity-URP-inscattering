using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HL.URPInscattering
{
	public class LineIntersector : MonoBehaviour
	{
		private Vector3 rotateVector(Vector3 rd, Vector3 rotation)
		{
			float radX = rotation.x * Mathf.Deg2Rad;
			float radY = rotation.y * Mathf.Deg2Rad;
			float radZ = rotation.z * Mathf.Deg2Rad;
			float sinX = Mathf.Sin(radX);
			float cosX = Mathf.Cos(radX);
			float sinY = Mathf.Sin(radY);
			float cosY = Mathf.Cos(radY);
			float sinZ = Mathf.Sin(radZ);
			float cosZ = Mathf.Cos(radZ);

			Vector3 xAxis = new Vector3(
				cosY * cosZ,
				cosX * sinZ + sinX * sinY * cosZ,
				sinX * sinZ - cosX * sinY * cosZ
			);
			Vector3 yAxis = new Vector3(
				-cosY * sinZ,
				cosX * cosZ - sinX * sinY * sinZ,
				sinX * cosZ + cosX * sinY * sinZ
			);
			Vector3 zAxis = new Vector3(
				sinY,
				-sinX * cosY,
				cosX * cosY
			);

			return xAxis * rd.x + yAxis * rd.y + zAxis * rd.z;
		}

		private void transformRay(Vector3 ro, Vector3 rd, out Vector3 outro, out Vector3 outrd, Vector3 offset, Vector3 rotation, float scale)
		{
			// offset
			outro = ro + offset;

			// rotation
			float radX = rotation.x * 0.01745329f;
			float radY = rotation.y * 0.01745329f;
			float radZ = rotation.z * 0.01745329f;
			float sinX = Mathf.Sin(radX);
			float cosX = Mathf.Cos(radX);
			float sinY = Mathf.Sin(radY);
			float cosY = Mathf.Cos(radY);
			float sinZ = Mathf.Sin(radZ);
			float cosZ = Mathf.Cos(radZ);

			Vector3 xAxis = new Vector3(
				cosY * cosZ,
				cosX * sinZ + sinX * sinY * cosZ,
				sinX * sinZ - cosX * sinY * cosZ
			);
			Vector3 yAxis = new Vector3(
				-cosY * sinZ,
				cosX * cosZ - sinX * sinY * sinZ,
				sinX * cosZ + cosX * sinY * sinZ
			);
			Vector3 zAxis = new Vector3(
				sinY,
				-sinX * cosY,
				cosX * cosY
			);

			outro /= scale;

			outro = xAxis * outro.x + yAxis * outro.y + zAxis * outro.z;
			outrd = xAxis * rd.x + yAxis * rd.y + zAxis * rd.z;
		}

		private bool rayConeIntersection(Vector3 rayPos, Vector3 rayDirection, out float near, out float far)
		{
			// default outputs
			near = 0;
			far = -1;

			// scale and offset into a unit cube
			rayPos.x += 0;
			//rayPos.x += 0.5;
			float s = 0.5f;
			rayPos.x *= s;
			rayDirection.x *= s;

			// quadratic x^2 = y^2 + z^2
			float a = rayDirection.y * rayDirection.y + rayDirection.z * rayDirection.z - rayDirection.x * rayDirection.x;
			float b = rayPos.y * rayDirection.y + rayPos.z * rayDirection.z - rayPos.x * rayDirection.x;
			float c = rayPos.y * rayPos.y + rayPos.z * rayPos.z - rayPos.x * rayPos.x;

			float cap = (s - rayPos.x) / rayDirection.x;

			// linear
			if (a == 0.0)
			{
				near = -0.5f * c / b;
				float x = rayPos.x + near * rayDirection.x;
				if (x < 0.0 || x > s)
					return false;

				far = cap;
				float tempp = Mathf.Min(far, near);
				far = Mathf.Max(far, near);
				near = tempp;
				return far > 0.0;
			}

			float delta = b * b - a * c;
			if (delta < 0.0f)
				return false;

			// 2 roots
			float deltasqrt = Mathf.Sqrt(delta);
			float arcp = 1.0f / a;
			near = (-b - deltasqrt) * arcp;
			far = (-b + deltasqrt) * arcp;

			// order roots
			float temp = Mathf.Min(far, near);
			far = Mathf.Max(far, near);
			near = temp;

			float xnear = rayPos.x + near * rayDirection.x;
			float xfar = rayPos.x + far * rayDirection.x;

			if (xnear < 0.0)
			{
				if (xfar < 0.0 || xfar > s)
					return false;

				near = far;
				far = cap;
			}
			else if (xnear > s)
			{
				if (xfar < 0.0 || xfar > s)
					return false;

				near = cap;
			}
			else if (xfar < 0.0)
			{
				// The apex is problematic,
				// additional checks needed to
				// get rid of the blinking tip here.
				far = near;
				near = cap;
			}
			else if (xfar > s)
			{
				far = cap;
			}

			return far > 0.0;
		}

		private Vector3 GetCameraPositionWS()
		{
			return Camera.main.transform.position;
		}

		private void OnDrawGizmos()
		{
			Vector3 cameraDirection = Camera.main.transform.forward;

			// Variables
			Vector3 volumetricLightPositionWS = transform.position;
			Vector3 volumetricLightRotation = transform.rotation.eulerAngles;
			float volumetricLightHeight = 5;

			// Cone volume intersection
			float near;
			float middle;
			float far;
			float through;
			Vector3 ro;
			Vector3 rd;

			ro = GetCameraPositionWS();
			rd = Camera.main.transform.forward;
			transformRay(GetCameraPositionWS(), cameraDirection, out ro, out rd, -volumetricLightPositionWS, -volumetricLightRotation, volumetricLightHeight);

			if (rayConeIntersection(ro, rd, out near, out far))
			{
				// near
				// far
				through = far - Mathf.Max(0, near);
				middle = Mathf.Lerp(Mathf.Max(0, near), far, 0.5f);

				if (through > 0)
				{
					cameraDirection *= 5;

					// Distances
					Vector3 volumePosNear = GetCameraPositionWS() + cameraDirection * near;
					Vector3 volumePosMiddle = GetCameraPositionWS() + cameraDirection * middle;
					Vector3 volumePosFar = GetCameraPositionWS() + cameraDirection * far;

					Gizmos.color = Color.red;
					Gizmos.DrawLine(GetCameraPositionWS(), volumePosFar);
					Gizmos.DrawSphere(volumePosFar, 0.02f);
					Gizmos.color = Color.yellow;
					Gizmos.DrawLine(GetCameraPositionWS(), volumePosMiddle);
					Gizmos.DrawSphere(volumePosMiddle, 0.02f);
					Gizmos.color = Color.green;
					Gizmos.DrawLine(GetCameraPositionWS(), volumePosNear);
					Gizmos.DrawSphere(volumePosNear, 0.02f);

					// Directions
					Gizmos.color = Color.blue;
					Gizmos.DrawRay(volumetricLightPositionWS, (volumePosMiddle - volumetricLightPositionWS).normalized);
				}
			}

			Gizmos.color = Color.white;
			Gizmos.DrawRay(volumetricLightPositionWS, rotateVector(new Vector3(1, 0, 0), volumetricLightRotation).normalized);
		}
	}
}
