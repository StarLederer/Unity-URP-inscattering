using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HL.URPInscattering
{
	public class Inscattering : ScriptableRendererFeature
	{
		class InscatteringPass : ScriptableRenderPass
		{
			// Constants
			const string k_RenderInscatteringTag = "Render Inscattering Pass";

			// Things
			Material m_InscatteringMaterial;
			RenderTargetIdentifier m_Source; // pointer to a Texture on the CPU
			RenderTargetHandle m_TempTexture; // pointer to a Texture on the GPU

			// Other things
			private Vector3[] frustumCorners;
			private Vector4[] vectorArray;

			// Constructor
			public InscatteringPass(Material material)
			{
				m_InscatteringMaterial = material;
				m_TempTexture.Init("_TempTexture");
			}

			public void SetSource(RenderTargetIdentifier source) => m_Source = source;

			public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
			{
				//1> Get command buffer
				CommandBuffer cmd = CommandBufferPool.Get(k_RenderInscatteringTag); // Name is for debugging

				//2> 
				RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor; // Get color texture parameters
																												   //cameraTextureDescriptor.depthBufferBits = 0; // remove depth
				cmd.GetTemporaryRT(m_TempTexture.id, cameraTextureDescriptor, FilterMode.Point);

				//3> Setup shader params
				var camera = renderingData.cameraData.camera;
				frustumCorners = new Vector3[4];
				vectorArray = new Vector4[4];

				camera.CalculateFrustumCorners(
					new Rect(0f, 0f, 1f, 1f),
					camera.farClipPlane,
					camera.stereoActiveEye,
					frustumCorners
				);
				vectorArray[0] = frustumCorners[0];
				vectorArray[1] = frustumCorners[3];
				vectorArray[2] = frustumCorners[1];
				vectorArray[3] = frustumCorners[2];
				m_InscatteringMaterial.SetVectorArray("_FrustumCorners", vectorArray);

				Matrix4x4 matrixCameraToWorld;
				Matrix4x4 matrixProjectionInverse;
				Matrix4x4 matrixHClipToWorld;
				if (camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono)
				{
					matrixCameraToWorld = camera.cameraToWorldMatrix;
					matrixProjectionInverse = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false).inverse;
					matrixHClipToWorld = matrixCameraToWorld * matrixProjectionInverse;
					m_InscatteringMaterial.SetMatrix("_MatrixScreenToWorldLeftEye", matrixHClipToWorld); // Left is the eye unity uses in shaders when in mono rendering so there is no need to make a separate matrix or set the right eye matrix at all

					m_InscatteringMaterial.SetMatrix("_MV", camera.worldToCameraMatrix);
					m_InscatteringMaterial.SetMatrix("_MP", GL.GetGPUProjectionMatrix(camera.projectionMatrix, false));
				}
				else
				{
					matrixCameraToWorld = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
					matrixProjectionInverse = GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), false).inverse;
					matrixHClipToWorld = matrixCameraToWorld * matrixProjectionInverse;
					m_InscatteringMaterial.SetMatrix("_MatrixScreenToWorldLeftEye", matrixHClipToWorld);

					matrixCameraToWorld = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;
					matrixProjectionInverse = GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), false).inverse;
					matrixHClipToWorld = matrixCameraToWorld * matrixProjectionInverse;
					m_InscatteringMaterial.SetMatrix("_MatrixScreenToWorldRightEye", matrixHClipToWorld);
				}

				//4> Blit for each InscatteringVolume
				m_InscatteringMaterial.EnableKeyword("_SPHERICAL_VOLUME");
#if !UNITY_EDITOR
				m_InscatteringMaterial.EnableKeyword("_FLIP_UV");
#endif
				foreach (InscatteringVolume volume in InscatteringVolumeManager.InscatteringVolumes)
				{
					cmd.SetGlobalVector("_VolumePosition", volume.transform.position);
					cmd.SetGlobalVector("_VolumeRotation", new Vector3(volume.transform.rotation.eulerAngles.x, volume.transform.rotation.eulerAngles.y - 90, volume.transform.rotation.eulerAngles.z));
					cmd.SetGlobalFloat("_VolumeRadius", volume.transform.localScale.x / 2f);
					cmd.SetGlobalColor("_InscatteringColor", volume.GetColor());
					Blit(cmd, m_TempTexture.Identifier(), m_Source, m_InscatteringMaterial, 0);
				}

				//5> Execute command buffer
				context.ExecuteCommandBuffer(cmd);
				CommandBufferPool.Release(cmd);
			}

			public override void FrameCleanup(CommandBuffer cmd)
			{
				cmd.ReleaseTemporaryRT(m_TempTexture.id);
			}
		}

		InscatteringPass m_InscatteringPass;

		public override void Create()
		{
			var material = new Material(Shader.Find("Hidden/Inscattering"));
			m_InscatteringPass = new InscatteringPass(material);
			m_InscatteringPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			m_InscatteringPass.SetSource(renderer.cameraColorTarget);
			renderer.EnqueuePass(m_InscatteringPass);
		}
	}
}
