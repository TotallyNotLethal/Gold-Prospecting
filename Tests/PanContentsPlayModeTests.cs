using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PanContentsPlayModeTests
{
    [UnityTest]
    public IEnumerator ShakingRemovesSedimentAndMakesGoldCollectible()
    {
        var panObject = new GameObject("Pan");
        var contents = panObject.AddComponent<PanContents>();
        contents.SetSedimentParticles(CreateSedimentParticles(10));
        var goldParticle = new PanContents.GoldParticle(5f, 19.3f, 0.5f, Vector3.zero);
        contents.SetGoldParticles(new[] { goldParticle });
        contents.GoldNuggetParent = panObject.transform;
        var prefab = new GameObject("NuggetPrefab");
        prefab.AddComponent<SphereCollider>();
        contents.GoldNuggetPrefab = prefab;

        var bodyObject = new GameObject("PanBody");
        var body = bodyObject.AddComponent<Rigidbody>();
        var analyzer = panObject.AddComponent<PanMotionAnalyzer>();
        analyzer.Contents = contents;
        analyzer.PanBody = body;

        body.angularVelocity = Vector3.one * (analyzer.ShakeThreshold + 1f);

        float elapsed = 0f;
        while (contents.SedimentNormalized > 0.2f && elapsed < 5f)
        {
            analyzer.Tick(0.25f);
            elapsed += 0.25f;
            yield return null;
        }

        Assert.Less(contents.SedimentCount, 10, "Shaking should remove sediment over time");
        Assert.IsTrue(goldParticle.IsRevealed, "Gold should be revealed once the threshold is met");

        bool collected = contents.TryCollectGold(goldParticle, out var collectedParticle);
        Assert.IsTrue(collected, "Revealed gold should be collectible");
        Assert.AreSame(goldParticle, collectedParticle);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(panObject);
        Object.DestroyImmediate(bodyObject);
    }

    private static IEnumerable<PanContents.SedimentParticle> CreateSedimentParticles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new PanContents.SedimentParticle(1f, 2.5f);
        }
    }
}
