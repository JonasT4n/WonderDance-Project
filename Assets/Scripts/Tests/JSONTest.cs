using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SimpleJSON;

public class JSONTest
{
    [SerializeField]
    private AudioSource audioTest = null;

    [Test]
    public void JSONTestSimplePasses()
    {
        JSONNode node = JSON.Parse("{}");
        node["0"] = JSONNode.Parse("{}");
        node["1"] = JSONNode.Parse("{}");
        Debug.Log($"In String: \n{node}");

        if (node["2"] == null)
        {
            node["2"] = JSONNode.Parse("{}");
        }

        Debug.Log($"In String: \n{node}");

        foreach (string childKey in node.Keys)
        {
            Debug.Log(childKey);
        }
    }

    [Test]
    public void ObjectComparison()
    {
        IObjInterface oc = new OveridenClass();
        Assert.AreEqual(oc is SampleBaseClass, true);
    }

    [Test]
    public void LoopIndexChange()
    {
        for (int i = 0; i < 12; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                Debug.Log($"Index: {i}-{j}");

                // Check jump
                if (i == 4 && j == 6)
                {
                    i = 10;
                    j = 12;
                }
            }
        }
    }

    [Test]
    public void TestStruct()
    {
        ABC abc = new ABC { val = 10, something = "Ayam" };
        ABC n = abc;
        abc.something = "Bebek";

        Debug.Log($"ABC Value: {abc.val} {abc.something}; N Value: {n.val} {n.something}");
        Assert.AreNotEqual(abc, n);
    }

    [Test]
    public void TestAudio()
    {

    }
}

public interface IObjInterface { }
public class SampleBaseClass { }
public class OveridenClass : SampleBaseClass, IObjInterface { }

public struct ABC
{
    public int val;
    public string something;
}