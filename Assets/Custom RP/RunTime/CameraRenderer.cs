using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    static ShaderTagId shaderTagId = new ShaderTagId("SRPDefaultUnlit");
    CullingResults cullingResults;
    const string bufferName = "Camera Render";
    CommandBuffer buffer = new CommandBuffer{ name = bufferName };
    ScriptableRenderContext context;
    Camera camera;
    public void Render(ScriptableRenderContext cont, Camera cam)
    {
        this.context = cont;
        this.camera = cam;
        PrepareBuffer();
        PrepareForSceneView();
        if (!Cull())
            return;

        Setup();
        DrawVisbleGeometry();
        DrawUnsuportedShaders();
        DrawGizmos();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisbleGeometry()
    {
        var sortSettings = new SortingSettings(camera){criteria = SortingCriteria.CommonOpaque};
        var drawingSettings = new DrawingSettings(shaderTagId, sortSettings);
        var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filterSettings);
        context.DrawSkybox(camera);
        sortSettings.criteria = SortingCriteria.CommonTransparent;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filterSettings);
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    // Start is called before the first frame update
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    // Update is called once per frame
    bool Cull()
    {
        if ( camera.TryGetCullingParameters(out ScriptableCullingParameters p) )
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
}
