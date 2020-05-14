using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum ActionType
{
    EightNeighbors = 8,
    Default = 1
}
public class PredictionVisualizer : MonoBehaviour
{
    public GameObject blockObject;
    public GameObject Goal;
    public Slider slider;
    public Text valText;

    public float beta = 10.0f;
    [SerializeField]
    private float updateIfBetaChangeLargerThan = 0.1f;
    [SerializeField]
    private float clampPossibility = 0.1f;
    [SerializeField]
    private int stepTime = 3;
    [SerializeField]
    public float gridWidth = 0.5f;
    [SerializeField]
    public int gridDimension = 20;
    [SerializeField]
    private GameObject host;
    private MDP mdp2D;
    private WaypointInput controller;

    private GameObject[] blocks;
    public bool[] blocks_index;
    [SerializeField]
    private Material baseMaterial;
    [SerializeField]
    private Color baseColor = new Color(0.0f, 1.0f, 0.0f);
    [SerializeField]
    private Color boundColor = new Color(1.0f, 0.0f, 0.0f);
    // Start is called before the first frame update
    void Start()
    {
        controller = host.GetComponent<WaypointInput>();
        // mdp2D = new MDP(gridWidth, gridDimension, stepTime);
        // mdp2D.init(Goal.transform.position, beta, updateIfBetaChangeLargerThan);
        // createBlocks();
        // setBeta();
    }

    public void Init()
    {
        mdp2D = new MDP(gridWidth, gridDimension, stepTime);
        mdp2D.init(Goal.transform.position, beta, updateIfBetaChangeLargerThan);
        createBlocks();
        setBeta();
    }

    // Update is called once per frame
    void Update()
    {
        mdp2D.updateBeta(beta, stepTime);
        drawBlocks();
    }

    private void createBlocks()
    {
        blocks = new GameObject[gridDimension * gridDimension];
        blocks_index = new bool[gridDimension * gridDimension];
        float offset = (gridWidth * gridDimension) / 2;
        const float GROUND_SNAP = -0.247f;
        for (int i = 0; i < gridDimension * gridDimension; i++)
        {
            Vector3 blockLoc = new Vector3(Mathf.Floor(i % gridDimension) * gridWidth - offset, GROUND_SNAP, Mathf.Floor(i / gridDimension) * gridWidth - offset);
            Vector3 centerOffset = new Vector3(gridWidth / 2, 0, gridWidth / 2);
            GameObject thisblock = Instantiate(blockObject, blockLoc + centerOffset, Quaternion.identity, this.transform);
            Material newMat = new Material(baseMaterial);
            thisblock.GetComponent<MeshRenderer>().material = newMat;
            thisblock.transform.localScale = new Vector3(gridWidth, 1f, gridWidth);
            blocks[i] = thisblock;
        }
    }

    private void drawBlocks()
    {
        float[] probs = mdp2D.predictThisState(host.transform.position);
        for (int i = 0; i < probs.Length; i++)
        {
            float height = probs[i];
            if (height > float.Epsilon + clampPossibility)
            {
                blocks_index[i] = true;
                blocks[i].transform.localScale = new Vector3(gridWidth, 1f, gridWidth);
                Color blend = baseColor * (1.0f - height) + boundColor * height;
                blocks[i].GetComponent<MeshRenderer>().material.SetColor("_BaseColor", blend);
                blocks[i].SetActive(true);
            } else
            {
                blocks_index[i] = false;
                blocks[i].SetActive(false);
            }
        }
    }

    public GameObject[] getBlocks()
    {
        return blocks;
    }

    public int getGridDimension()
    {
        return gridDimension;
    }

    public void setBeta()
    {
        beta = 0.01f * Mathf.Pow(1.0964781961f, (float)slider.value);
        valText.text = slider.value.ToString();
    }
}