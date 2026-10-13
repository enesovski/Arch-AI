using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DissolveEffect : MonoBehaviour
{

    private float dissolve = 0f;
    private bool dissolving = false;

    [SerializeField] private Renderer meshRenderer;
    private Material dissolveMaterial;
    [SerializeField] private GameObject parentObj;

    private void Start()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<Renderer>();
        }
        dissolveMaterial = meshRenderer.material;

        if (parentObj == null)
        {
            if (transform.parent != null)
            {
                parentObj = transform.parent.gameObject;
            }
            else
            {
                parentObj = gameObject; 
            }
        }
    }

    private void Update()
    {
        if (dissolving)
        {
            dissolve += Time.deltaTime;
            dissolveMaterial.SetFloat("_Dissolve", Mathf.InverseLerp(0.5f, 1.5f, dissolve));
        }
    }

    public void StartFading()
    {
        //dissolving = true;
        //Destroy(parentObj, 1.5f);
    }
}
