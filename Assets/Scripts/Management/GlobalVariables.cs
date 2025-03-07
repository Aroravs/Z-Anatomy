using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GlobalVariables : MonoBehaviour
{
    [HideInInspector]
    public static GlobalVariables Instance;

    [SerializeField]
    private Color _highligthColor;
    [SerializeField]
    private Color _secondaryColor;
    [SerializeField]
    private Color _surfaceColor;
    [SerializeField]
    private Color _onSurfaceColor;
    [SerializeField]
    private Color _backgroundColor;
    [SerializeField]
    private Color _iconColor;
    [SerializeField]
    private Color _disabledIconColor;
    [SerializeField]
    private Color _taskBarColor;

    public float labelFontSize;
    public float titleLabelFontSize;
    public float lineSize;

    public static Color HighligthColor;
    public static Color SecondaryColor;
    public static Color SurfaceColor;
    public static Color OnSurfaceColor;
    public static Color BackgroundColor;
    public static Color IconColor;
    public static Color DisabledIconColor;
    public static Color TaskBarColor;

    public bool refresh;

    public GameObject globalParent;
    [HideInInspector]
    public List<NameAndDescription> allNameScripts;
    [HideInInspector]
    public List<MeshRenderer> allBodyPartRenderers;
    [HideInInspector]
    public List<Visibility> allVisibilityScripts;
    [HideInInspector]
    public List<BodyPart> allBodyParts;
    [HideInInspector]
    public List<GameObject> bodySections;

    [HideInInspector]
    public List<BodyPart> bones;
    [HideInInspector]
    public List<BodyPart> insertions;
    [HideInInspector]
    public Dictionary<string, BodyPart> insertionsDictionary = new Dictionary<string, BodyPart>();
    [HideInInspector]
    public Dictionary<string, BodyPart> musclesDictionary = new Dictionary<string, BodyPart>();
    [HideInInspector]
    public List<BodyPart> joints;
    [HideInInspector]
    public List<BodyPart> muscles;
    [HideInInspector]
    public List<BodyPart> lymphs;
    [HideInInspector]
    public List<BodyPart> arteries;
    [HideInInspector]
    public List<BodyPart> veins;
    [HideInInspector]
    public List<BodyPart> nerves;
    [HideInInspector]
    public List<BodyPart> viscera;
    [HideInInspector]
    public List<BodyPart> regions;
    [HideInInspector]
    public List<BodyPart> references;

    private void Awake()
    {
        Instance = this;
        Build();

        allNameScripts = globalParent.GetComponentsInChildren<NameAndDescription>(true).ToList();
        allBodyPartRenderers = globalParent.GetComponentsInChildren<MeshRenderer>(true).Where(it => it.GetComponent<Label>() == null && it.GetComponent<Line>() == null && !it.gameObject.name.Contains(".g")).ToList();
        allVisibilityScripts = globalParent.GetComponentsInChildren<Visibility>(true).ToList();
        allBodyParts = globalParent.GetComponentsInChildren<BodyPart>(true).ToList();
        
        bones = allBodyParts.Where(it => it.CompareTag("Skeleton")).ToList();
        insertions = allBodyParts.Where(it => it.CompareTag("Insertions")).ToList();
        joints = allBodyParts.Where(it => it.CompareTag("Joints")).ToList();
        muscles = allBodyParts.Where(it => it.CompareTag("Muscles")).ToList();
        lymphs = allBodyParts.Where(it => it.CompareTag("Lymph")).ToList();
        arteries = allBodyParts.Where(it => it.CompareTag("Arteries")).ToList();
        veins = allBodyParts.Where(it => it.CompareTag("Veins")).ToList();
        nerves = allBodyParts.Where(it => it.CompareTag("Nervous")).ToList();
        viscera = allBodyParts.Where(it => it.CompareTag("Visceral")).ToList();
        regions = allBodyParts.Where(it => it.CompareTag("BodyParts")).ToList();
        references = allBodyParts.Where(it => it.CompareTag("References")).ToList();
        

        foreach (Transform section in globalParent.transform)
            bodySections.Add(section.gameObject);

    }

    private void Start()
    {
        foreach (var insertion in insertions)
            insertionsDictionary.Add(insertion.nameScript.originalName, insertion);

        foreach (var muscle in muscles)
            musclesDictionary.Add(muscle.nameScript.originalName, muscle);
    }

    private void OnValidate()
    {
        if(Instance == null)
            Instance = this;
        if (refresh)
        {
            refresh = false;
            Build();
        }
    }

    private void Build()
    {

        HighligthColor = _highligthColor;
        SecondaryColor = _secondaryColor;
        SurfaceColor = _surfaceColor;
        OnSurfaceColor = _onSurfaceColor;
        BackgroundColor = _backgroundColor;
        IconColor = _iconColor;
        DisabledIconColor = _disabledIconColor;
        TaskBarColor = _taskBarColor;

        SetSecondaryColor[] secondaryElements = FindObjectsOfType<SetSecondaryColor>();
        SetSurfaceColor[] surfaceElements = FindObjectsOfType<SetSurfaceColor>();
        SetTaskbarColor taskbar = FindObjectOfType<SetTaskbarColor>();

        foreach (var item in secondaryElements)
            item.GetComponent<Image>().color = SecondaryColor;

        foreach (var item in surfaceElements)
            item.GetComponent<Image>().color = SurfaceColor;

        if (Camera.main != null)
            Camera.main.backgroundColor = BackgroundColor;
        if (taskbar != null)
            taskbar.GetComponent<Image>().color = TaskBarColor;
    }


}
