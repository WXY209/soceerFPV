using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//*****************************************
//创建人：
//功能说明：无人机尾灯镜头光晕的动态效果
//***************************************** 
public class RLensFlare : MonoBehaviour
{
    public LensFlareComponentSRP flare;

    void Start()
    {
        flare.lensFlareData = Instantiate(flare.lensFlareData);
        float randomAngle = Random.Range(0f, 360f);
        foreach (var e in flare.lensFlareData.elements)
            e.rotation = randomAngle;
    }

    void Update()
    {
        flare.scale = 1f + 1.5f * Mathf.PingPong(Time.time / 2.5f, 1f);
        foreach (var e in flare.lensFlareData.elements) 
        {
            e.rotation += Time.deltaTime * 3f;
            if (e.rotation > 360f) e.rotation -= 360f;
        }
    }
    private void OnDestroy()
    {
        if (flare != null && flare.lensFlareData != null)
        {
            DestroyImmediate(flare.lensFlareData);
        }
    }
}