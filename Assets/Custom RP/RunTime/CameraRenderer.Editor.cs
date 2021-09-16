using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

partial class CameraRenderer
{
    partial void PrepareBuffer();
    partial void PrepareForSceneView();
    partial void DrawGizmos();
    partial void DrawUnsuportedShaders();
#if UNITY_EDITOR
    string SampleName{get;set;}
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    static Material unityErrorMaterial;

    partial void DrawUnsuportedShaders()
    {
        if(unityErrorMaterial == null)
            unityErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)){ overrideMaterial = unityErrorMaterial };
        for (int i = 1; i < legacyShaderTagIds.Length; ++i)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        var filterSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filterSettings);
    }

    partial void DrawGizmos()
    {
        if(Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneView()
    {
        if(camera.cameraType == CameraType.SceneView)
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
    }

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    string SampleName => bufferName;
#endif
}
