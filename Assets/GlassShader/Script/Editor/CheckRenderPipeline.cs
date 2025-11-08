
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;

namespace GlassShader.Script.Editor
{
    [InitializeOnLoad]
    public static class RenderPipelineDefines
    {
        static  string RenderPipipelineMacro_urp = "GLASSSHADER_USING_URP";
        static  string RenderPipipelineMacro_hdrp = "GLASSSHADER_USING_HDRP";
        static string RenderPipipelineMacro_buildIn = "GLASSSHADER_USING_BUILTIN";
        static RenderPipelineDefines()
        {
            // Các nhóm build target phổ biến
            BuildTargetGroup[] targetGroups =
            {
                BuildTargetGroup.Standalone,
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.WebGL,
                BuildTargetGroup.PS4,
                BuildTargetGroup.PS5,
                BuildTargetGroup.XboxOne,
                BuildTargetGroup.GameCoreXboxSeries
            };

            string macro = DetectPipelineMacro();

            foreach (var group in targetGroups)
            {
                if (group == BuildTargetGroup.Unknown) continue;

                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

                // Xóa các macro cũ
                symbols = symbols.Replace(RenderPipipelineMacro_urp, "")
                    .Replace(RenderPipipelineMacro_hdrp, "")
                    .Replace(RenderPipipelineMacro_buildIn, "");

                // Thêm macro mới nếu chưa có
                if (!symbols.Split(';').Contains(macro))
                    symbols = (symbols.Trim(';') + ";" + macro).Trim(';');

                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
            }
        }
//
        private static string DetectPipelineMacro()
        {
            var rpAsset = GraphicsSettings.defaultRenderPipeline;

            if (rpAsset == null)
                return RenderPipipelineMacro_buildIn;
            else if (rpAsset.GetType().ToString().Contains("Universal"))
                return RenderPipipelineMacro_urp;
            else if (rpAsset.GetType().ToString().Contains("HD"))
                return RenderPipipelineMacro_hdrp;
            else
                return RenderPipipelineMacro_buildIn;
        }
    }
}
