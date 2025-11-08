// using UnityEngine;
// using UnityEngine.Rendering;
//
// namespace GlassShader.Script.BuildIn
// {
//     [ExecuteInEditMode]
//     public class CustomRenderObject : MonoBehaviour
//     {
//         public Material overrideMaterial; // Material bạn muốn dùng để render
//         public CanvasRenderer targetMesh;     // MeshFilter của object muốn render
//         public Camera targetCamera;       // Camera sẽ render lại object
//         public CameraEvent targetCameraEvent;
//         private CommandBuffer cmdBuffer;
//
//         void OnEnable()
//         {
//             if (targetCamera == null)
//                 targetCamera = Camera.main;
//
//             //// Tạo CommandBuffer mới
//             cmdBuffer = new CommandBuffer();
//             cmdBuffer.name = "Custom Render Object";
//
//             // Gắn vào camera ở sự kiện BeforeImageEffects
//             targetCamera.AddCommandBuffer(targetCameraEvent, cmdBuffer);
//         }
//
//         void OnDisable()
//         {
//             if (targetCamera != null && cmdBuffer != null)
//             {
//                 targetCamera.RemoveCommandBuffer(targetCameraEvent, cmdBuffer);
//             }
//         }
//
//         void LateUpdate()
//         {
//             if (cmdBuffer == null || overrideMaterial == null || targetMesh == null) return;
//
//             // Xóa lệnh cũ
//             Canvas.ForceUpdateCanvases();
//             cmdBuffer.Clear();
//             cmdBuffer.SetViewProjectionMatrices(targetCamera.worldToCameraMatrix, targetCamera.projectionMatrix);
//             // Render mesh với material override
//             cmdBuffer.DrawMesh(
//                 targetMesh.GetMesh(),
//                 targetMesh.transform.localToWorldMatrix,
//                 overrideMaterial,
//                 0,0
//             );
//             cmdBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity); // reset nếu cần
//
//         }
//     }
// }