using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CityBackfaceifier : MonoBehaviour
{
    [Tooltip("Don't put this on the city itself, unless you like recursion.")]
    [SerializeField] private GameObject city;
    [SerializeField] private LayerNumber layer;
    [SerializeField] private Material material;

    private Dictionary<Material, Material> materialUpdateDictionary;
    
    private void Recurse(Transform t)
    {
        t.gameObject.layer = layer.value;
        
        foreach (Collider collider in t.GetComponents<Collider>())
        {
            Destroy(collider);
        }

        MeshRenderer renderer = t.GetComponent<MeshRenderer>();
        if (renderer)
        {
            List<Material> newMaterials = new List<Material>(renderer.materials.Length);
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                Material oldMaterial = renderer.materials[i];
                
                if (!materialUpdateDictionary.TryGetValue(oldMaterial, out Material newMaterial))
                {
                    newMaterial = new Material(material);
                    newMaterial.name = "BF_" + oldMaterial.name;
                    newMaterial.CopyPropertiesFromMaterial(oldMaterial);
                    materialUpdateDictionary.Add(oldMaterial, newMaterial);
                }
                
                newMaterials.Add(newMaterial);
            }
            renderer.SetMaterials(newMaterials);
        }
        
        foreach (Transform child in t)
        {
            Recurse(child);
        }
    }
    
    void Awake()
    {
        if (this.enabled)
        {
            GameObject clone = Instantiate(city, city.transform.parent);
            clone.transform.SetSiblingIndex(city.transform.GetSiblingIndex() + 1);

            materialUpdateDictionary = new Dictionary<Material, Material>();

            Recurse(clone.transform);
        }

        Destroy(this);
    }
}
