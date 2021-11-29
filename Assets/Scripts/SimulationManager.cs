using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UImGui;
using ImGuiNET;
using System.Diagnostics;

public class SimulationManager : MonoBehaviour
{
    public int numBarInterations;
    public float timeStep;
    public float barLength = 6;
    //
    public GameObject originalPointObject;
    public GameObject originalLineObject;

    public Cloth cloth;
    public Rope rope;

    [HideInInspector]

    public Point[,] PointMatrix;

    private int clothColumns = 34;
    private int clothRows = 34;
    private float clothBarLength = 0.3f;

    private int ropePoints = 5;
    private float ropeBarLength = 4f;

    private string label = "";
    private float count;
    private bool simulationStartedOnce = false;


    private void Awake()
    {
        //
        UImGuiUtility.Layout += OnLayout;
        UImGuiUtility.OnInitialize += OnInitialize;
        UImGuiUtility.OnDeinitialize += OnDeinitialize;
        simulationStartedOnce = false;
    }

    IEnumerator Start()
    {
        while (true)
        {
            if (Time.timeScale == 1)
            {
                yield return new WaitForSeconds(0.1f);
                count = (1 / Time.deltaTime);
                label = "FPS :" + (Mathf.Round(count));
            }
            else
            {
                label = "Pause";
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                Transform objectHit = hit.transform;
                if (!objectHit.name.Contains("|"))
                {
                    string number = objectHit.name.Substring(5);
                    UnityEngine.Debug.Log(number);
                    int index = int.Parse(number);
                    rope.points[index].locked = !rope.points[index].locked;
                    rope.points[index].UpdateLocked();
                }
                else
                {
                    string number = objectHit.name.Substring(5);
                    string[] split = number.Split('|');

                    int index1 = int.Parse(split[0]);
                    int index2 = int.Parse(split[1]);

                    UnityEngine.Debug.Log($"{index1}|{index2}");
                    //
                    cloth.pointMatrix[index1, index2].locked = !cloth.pointMatrix[index1, index2].locked;
                    cloth.pointMatrix[index1, index2].UpdateLocked();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        if(Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        //Simulate(Time.deltaTime);
        //SimulateCloth(5, 3, Time.deltaTime);
        if (simulationStartedOnce)
        {
            cloth.Simulate(Time.deltaTime);
            rope.Simulate(Time.deltaTime);
        }
    }


    private void OnInitialize(UImGui.UImGui obj)
    {
        // runs after UImGui.OnEnable();
    }

    private void OnLayout(UImGui.UImGui obj)
    {
        if (!simulationStartedOnce)
        {
            ImGui.Begin("START", ImGuiWindowFlags.AlwaysAutoResize);
            if (ImGui.Button("START SIMULATIONS"))
            {
                cloth = new Cloth(clothRows, clothColumns, 5, clothBarLength, originalPointObject, originalLineObject);
                cloth.useDiagonal = true;
                //
                rope = new Rope(ropePoints, 20, ropeBarLength, originalPointObject, originalLineObject);
                simulationStartedOnce = true;
            }
            ImGui.End();
        }
        else
        {
            // Unity Update method. 
            ImGui.Begin("Cloth Simulation Start Settings", ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.InputInt("Cloth Column Count", ref clothRows);
            ImGui.InputInt("Cloth Row Count", ref clothColumns);
            ImGui.DragFloat("Cloth Bar Length", ref clothBarLength, 0.1f);
            if (ImGui.Button("RESET CLOTH SIMULATION"))
            {
                Destroy(cloth.root);
                cloth.columnNum = clothColumns;
                cloth.rowNum = clothRows;
                cloth.barLength = clothBarLength;
                cloth.Initialize();
            }
            ImGui.End();
            //
            ImGui.Begin("Rope Simulation Start Settings", ImGuiWindowFlags.AlwaysAutoResize);
            if (ImGui.InputInt("Rope Point Count", ref ropePoints))
            {
                if (ropePoints <= 1) ropePoints = 2;
            }
            ImGui.DragFloat("Rope Bar Length", ref ropeBarLength, 0.1f);
            if (ImGui.Button("RESET ROPE SIMULATION"))
            {
                Destroy(rope.root);
                rope.pointsNum = ropePoints;
                rope.barLength = ropeBarLength;
                rope.Initialize();
            }
            ImGui.End();
            //
            bool fps = true;
            ImGui.SetNextWindowBgAlpha(0.0f);
            ImGui.Begin("FPS", ref fps, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), label);
            ImGui.End();
            //
            cloth.RenderImGui();
            rope.RenderImGui();
        }
    }

    private void OnDeinitialize(UImGui.UImGui obj)
    {
        // runs after UImGui.OnDisable();
    }

    private void OnDisable()
    {
        UImGuiUtility.Layout -= OnLayout;
        UImGuiUtility.OnInitialize -= OnInitialize;
        UImGuiUtility.OnDeinitialize -= OnDeinitialize;
    }

    // IEnumerator SimulationCoroutine(float timeStep)
    // {
    //     while (true)
    //     {
    //         Simulate(timeStep);
    //         yield return new WaitForSecondsRealtime(timeStep);
    //     }
    // }
}

[System.Serializable]
public class Point
{
    public string name;
    public Vector3 position, prevPosition;
    public float mass;
    public bool locked;
    public Vector3 sumForce;
    public GameObject gameObject;

    public Point(Vector3 position, float mass, bool locked, Vector3 sumForce)
    {
        this.position = position;
        this.prevPosition = position;
        this.mass = mass;
        this.locked = locked;
        this.sumForce = sumForce;
    }

    public void ChangeForceX(float amount)
    {
        sumForce.x = amount;
    }

    public void ChangeForceY(float amount)
    {
        sumForce.y = amount;
    }

    public void ChangeForceZ(float amount)
    {
        sumForce.z = amount;
    }


    public void UpdateLocked()
    {
        gameObject.GetComponent<MeshRenderer>().material.color = locked ? Color.red : Color.gray;
    }
}

[System.Serializable]
public class Bar
{
    public Point pointA, pointB;
    public float length;
    public LineRenderer line;
    public bool diagonal;

    public void UpdateLine()
    {
        line.SetPosition(0, pointA.position);
        line.SetPosition(1, pointB.position);
    }

    public Bar(Point pointA, Point pointB, float length)
    {
        this.pointA = pointA;
        this.pointB = pointB;
        this.length = length;
        this.diagonal = false;
    }
}

[System.Serializable]
public class Cloth
{
    public Point[,] pointMatrix;

    public List<Bar> bars;

    public int rowNum;
    public int columnNum;
    public int numBarInterations;
    public float barLength;
    public bool useDiagonal;
    public bool renderLines;
    public float CPUTime;

    public GameObject originalPointObject;

    public GameObject originalLineObject;
    public GameObject root;
    public LineRenderer line;

    private Vector3 forceOnPoints;
    private Stopwatch sw;

    public Cloth(int rowNum, int columnNum, int numBarInterations, float barLength, GameObject originalPointObject, GameObject originalLineObject)
    {
        this.rowNum = rowNum;
        this.columnNum = columnNum;
        this.numBarInterations = numBarInterations;
        this.barLength = barLength;
        this.bars = new List<Bar>();
        this.originalLineObject = originalLineObject;
        this.originalPointObject = originalPointObject;
        this.forceOnPoints = new Vector3(165.0f, -15, 36.0f);
        this.sw = new Stopwatch();
        ///////
        Initialize();

        this.renderLines = true;
    }

    public void Initialize()
    {
        pointMatrix = new Point[rowNum, columnNum];
        int barCount = 0;
        float sqrtTwo = Mathf.Sqrt(2);
        if (this.bars.Count > 0) this.bars.Clear();
        root = new GameObject("Cloth");
        //
        for (int i = 0; i < rowNum; i++)
        {
            for (int j = 0; j < columnNum; j++)
            {
                Point newPoint = new Point(new Vector3(i * barLength, j * -barLength, 0), 1, j % 4 == 0 && i == 0, forceOnPoints);
                newPoint.name = $"Point {i}|{j}";
                pointMatrix[i, j] = newPoint;
            }
        }

        for (int i = 0; i < rowNum; i++)
        {
            for (int j = 0; j < columnNum; j++)
            {
                if (j != columnNum - 1)
                {
                    bars.Add(new Bar(pointMatrix[i, j], pointMatrix[i, j + 1], barLength));
                    barCount++;
                }

                if (j != 0 && i != rowNum - 1) // Diagonal
                {
                    Bar newBar = new Bar(pointMatrix[i, j], pointMatrix[i + 1, j - 1], barLength * sqrtTwo);
                    newBar.diagonal = true;
                    bars.Add(newBar);
                    barCount++;
                }

                if (i != rowNum - 1)
                {
                    bars.Add(new Bar(pointMatrix[i, j], pointMatrix[i + 1, j], barLength));
                    //                    
                }

                if (j != columnNum - 1 && i != rowNum - 1) // Diagonal
                {
                    Bar newBar = new Bar(pointMatrix[i, j], pointMatrix[i + 1, j + 1], barLength * sqrtTwo);
                    newBar.diagonal = true;
                    bars.Add(newBar);
                    // GameObject lineCross1Obj = GameObject.Instantiate(originalLineObject, Vector3.zero, Quaternion.identity);
                    // bars[barCount].line = lineCross1Obj.GetComponent<LineRenderer>();
                    // bars[barCount].UpdateLine();
                    barCount++;
                }
            }
        }

        GameObject lineSideObj = GameObject.Instantiate(originalLineObject, Vector3.zero, Quaternion.identity);
        lineSideObj.transform.SetParent(root.transform);
        line = lineSideObj.GetComponent<LineRenderer>();
        line.positionCount = bars.Count * 2;

        for (int i = 0; i < rowNum; i++)
        {
            for (int j = 0; j < columnNum; j++)
            {
                if (pointMatrix[i, j].gameObject == null)
                {
                    pointMatrix[i, j].gameObject = GameObject.Instantiate(originalPointObject, pointMatrix[i, j].position, Quaternion.identity);
                    pointMatrix[i, j].gameObject.name = pointMatrix[i, j].name;
                    pointMatrix[i, j].gameObject.transform.SetParent(root.transform);
                    pointMatrix[i, j].UpdateLocked();
                }
            }
        }

        for (int i = 0; i < 50; i++)
        {
            foreach (Bar bar in bars)
            {
                Vector3 centerBar = (bar.pointA.position + bar.pointB.position) / 2;
                Vector3 dirBar = (bar.pointA.position - bar.pointB.position).normalized;
                if (!bar.pointA.locked)
                {
                    bar.pointA.position = centerBar + dirBar * bar.length / 2;
                }
                if (!bar.pointB.locked)
                {
                    bar.pointB.position = centerBar - dirBar * bar.length / 2;
                }
            }
        }
    }

    public void Simulate(float h)
    {
        sw.Reset();
        sw.Start();
        for (int i = 0; i < rowNum; i++)
        {
            for (int j = 0; j < columnNum; j++)
            {
                Point p = pointMatrix[i, j];
                if (!p.locked)
                {
                    Vector3 nextPosition;
                    nextPosition = p.position + (p.position - p.prevPosition) + (h * h / p.mass) * p.sumForce;
                    p.prevPosition = p.position;
                    p.position = nextPosition;
                }
            }
        }


        for (int i = 0; i < numBarInterations; i++)
        {
            foreach (Bar bar in bars)
            {
                if (bar.diagonal && !this.useDiagonal) continue;

                Vector3 centerBar = (bar.pointA.position + bar.pointB.position) / 2;
                Vector3 dirBar = (bar.pointA.position - bar.pointB.position).normalized;
                if (!bar.pointA.locked)
                {
                    bar.pointA.position = centerBar + dirBar * bar.length / 2;
                }
                if (!bar.pointB.locked)
                {
                    bar.pointB.position = centerBar - dirBar * bar.length / 2;
                }
            }
        }
        sw.Stop();
        CPUTime = sw.ElapsedMilliseconds;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < rowNum; i++)
        {
            for (int j = 0; j < columnNum; j++)
            {
                Point p = pointMatrix[i, j];
                p.gameObject.transform.position = p.position;
            }
        }

        if (!renderLines)
        {
            line.gameObject.SetActive(false);
        }
        else
        {
            line.gameObject.SetActive(true);
            int lineIndex = 0;
            for (int i = 0; i < bars.Count; i++)
            {
                line.SetPosition(lineIndex, bars[i].pointA.position);
                lineIndex++;
                line.SetPosition(lineIndex, bars[i].pointB.position);
                lineIndex++;
            }
        }

    }

    public void ChangeForceCloth(float x, float y, float z)
    {
        foreach (Point point in pointMatrix)
        {
            point.ChangeForceX(x);
            point.ChangeForceY(y);
            point.ChangeForceZ(z);
        }
    }

    public void ChangeBarLength(float newLength)
    {
        float sqrtTwo = Mathf.Sqrt(2);
        foreach (Bar bar in bars)
        {
            if (bar.diagonal)
            {
                bar.length = sqrtTwo * newLength;
            }
            else
            {
                bar.length = newLength;
            }
        }
    }

    public void RenderImGui()
    {
        ImGui.Begin("Cloth", ImGuiWindowFlags.AlwaysAutoResize);
        if (ImGui.DragFloat("Force X", ref forceOnPoints.x, 1))
        {
            ChangeForceCloth(forceOnPoints.x, forceOnPoints.y, forceOnPoints.z);
        }
        if (ImGui.DragFloat("Force Y", ref forceOnPoints.y, 1))
        {
            ChangeForceCloth(forceOnPoints.x, forceOnPoints.y, forceOnPoints.z);
        }
        if (ImGui.DragFloat("Force Z", ref forceOnPoints.z, 1))
        {
            ChangeForceCloth(forceOnPoints.x, forceOnPoints.y, forceOnPoints.z);
        }
        ImGui.DragInt("Number of Iterrations",ref numBarInterations, 1,4,200);
        ImGui.Checkbox("Render Lines", ref renderLines); ImGui.SameLine();
        ImGui.Text($"CPU Simulation time {CPUTime} ms");
        ImGui.End();
    }
}

[System.Serializable]
public class Rope
{
    public List<Point> points;

    public List<Bar> bars;

    public int pointsNum;
    public int numBarInterations;
    public float barLength;
    public GameObject originalPointObject;
    public GameObject originalLineObject;
    public GameObject root;
    public Vector3 forceOnPoints;

    public Rope(int pointsNum, int numBarInterations, float barLength, GameObject originalPointObject, GameObject originalLineObject)
    {
        this.points = new List<Point>();
        this.bars = new List<Bar>();
        this.pointsNum = pointsNum;
        this.numBarInterations = numBarInterations;
        this.barLength = barLength;
        this.originalPointObject = originalPointObject;
        this.originalLineObject = originalLineObject;
        this.forceOnPoints = new Vector3(0, -15, 0);
        /////
        Initialize();
    }

    public void Initialize()
    {
        if (this.bars.Count > 0) this.bars.Clear();
        if (this.points.Count > 0) this.points.Clear();
        root = new GameObject("Rope");
        //
        int barCount = 0;
        for (int i = 0; i < pointsNum; i++)
        {
            Point newPoint = new Point(new Vector3(i * barLength, i * -barLength, 0) + Vector3.left * 10f, 1, i == 0, forceOnPoints);
            newPoint.name = $"Point {i}";
            this.points.Add(newPoint);
        }

        for (int i = 0; i < pointsNum; i++)
        {
            if (i != pointsNum - 1)
            {
                bars.Add(new Bar(points[i], points[i + 1], barLength)); // TODO Change this to only one Line Renderer
                GameObject lineSideObj = GameObject.Instantiate(originalLineObject, Vector3.zero, Quaternion.identity);
                lineSideObj.transform.SetParent(root.transform);
                bars[barCount].line = lineSideObj.GetComponent<LineRenderer>();
                bars[barCount].UpdateLine();
                barCount++;
            }
        }

        for (int i = 0; i < pointsNum; i++)
        {
            points[i].gameObject = GameObject.Instantiate(originalPointObject, points[i].position, Quaternion.identity);
            points[i].gameObject.name = points[i].name;
            points[i].gameObject.transform.SetParent(root.transform);
            points[i].UpdateLocked();
        }
    }

    public void ChangeForce(float x, float y, float z)
    {
        foreach (Point point in points)
        {
            point.ChangeForceX(x);
            point.ChangeForceY(y);
            point.ChangeForceZ(z);
        }
    }

    public void RenderImGui()
    {
        ImGui.Begin("Rope", ImGuiWindowFlags.AlwaysAutoResize);
        if (ImGui.DragFloat("Force X", ref forceOnPoints.x, 1))
        {
            ChangeForce(forceOnPoints.x, forceOnPoints.y, forceOnPoints.z);
        }
        if (ImGui.DragFloat("Force Y", ref forceOnPoints.y, 1))
        {
            ChangeForce(forceOnPoints.x, forceOnPoints.y, forceOnPoints.z);
        }
        if (ImGui.DragFloat("Force Z", ref forceOnPoints.z, 1))
        {
            ChangeForce(forceOnPoints.x, forceOnPoints.y, forceOnPoints.z);
        }
    }


    public void Simulate(float h)
    {
        // nextPos = currentPosition + (1- amort)(currentPosition - lastPosition) + h*h / mass * sumForce;
        foreach (Point p in points)
        {
            if (!p.locked)
            {
                Vector3 nextPosition;
                nextPosition = p.position + (p.position - p.prevPosition) + (h * h / p.mass) * p.sumForce;
                p.prevPosition = p.position;
                p.position = nextPosition;
            }
        }

        for (int i = 0; i < numBarInterations; i++)
        {
            foreach (Bar bar in bars)
            {
                Vector3 centerBar = (bar.pointA.position + bar.pointB.position) / 2;
                Vector3 dirBar = (bar.pointA.position - bar.pointB.position).normalized;
                if (!bar.pointA.locked)
                {
                    bar.pointA.position = centerBar + dirBar * bar.length / 2;
                }
                if (!bar.pointB.locked)
                {
                    bar.pointB.position = centerBar - dirBar * bar.length / 2;
                }
            }
        }

        foreach (Point p in points)
        {
            p.gameObject.transform.position = p.position;
        }
        foreach (Bar bar in bars)
        {
            bar.UpdateLine();
        }
    }
}