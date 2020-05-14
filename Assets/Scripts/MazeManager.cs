using System.Collections.Generic;
using UnityEngine;
using System;

public class MazeManager : MonoBehaviour
{

    public GameObject pathMarker;
    public GameObject prediction;
    public GameObject carGoal;
    private Rigidbody rb;
    public float speed = 1f;
    
    private PredictionVisualizer predictionVisualizer;

    public Car car;
    public GameObject pedestrian;

    private Astar astar;

    public List<GameObject> obstacle_objects;

    public List<Vector2Int> obstacles;

    public List<Vector2> unsafeRegion;

    public static int gridNumber = 80;

    [HideInInspector]
    public int cols, rows;

    [HideInInspector]
    public static float gridWidth = 0.5f;

    public int safeRegionSize = 0;

    public Camera cam;
    public GameObject panel;
    private Vector2 dest;
    private bool stop;

    // Start is called before the first frame update
    void Start()
    {
        // Set car with goals
        //List<Vector2> carGoals = new List<Vector2>();
        //carGoals.Add(new Vector2(0, 15));
        //carGoals.Add(new Vector2(10, -10));
        dest = new Vector2(carGoal.transform.position.x, carGoal.transform.position.z);

        cols = gridNumber;
        rows = gridNumber;

        Time.timeScale = 1f;
        stop = false;
        List<Vector2Int> carGoal_grid = new List<Vector2Int>();
        carGoal_grid.Add(world_to_grid(dest));
        //foreach (Vector2 goal in carGoals)
        //{
        //    carGoal_grid.Add(world_to_grid(goal));
        //}

        GameObject carObj = GameObject.Find("car");

        this.car = new Car(carObj, carGoal_grid);

        // Set obstacles
        setInitialObstacles();

        predictionVisualizer = prediction.GetComponent<PredictionVisualizer>();
        predictionVisualizer.gridDimension = gridNumber;
        predictionVisualizer.gridWidth = gridWidth;
        predictionVisualizer.Init();
        // Set pedestrian

        // Astar
        this.astar = new Astar();

        rb = car.c.GetComponent<Rigidbody>();

        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = 0.15f;
    }

    // Update is called once per frame
    void Update()
    {
        this.car.update();
        UpdateObstacles();
        
        // Check if car hit goal
        if(car.current_grid == car.goals[0])
        {
            //car.goals.RemoveAt(0);
            //if (car.goals.Count == 0)
            //{
            //    QuitGame();
            //}
            Time.timeScale = 0f;
            panel.SetActive(true);
        }
        
        List<Vector2Int> path = astar.search(this);
        // Show path
        
        GameObject[] paths;
        paths = GameObject.FindGameObjectsWithTag("Marker");
        if (paths != null)
        {
            foreach (GameObject marker in paths)
            {
                Destroy(marker);
            }
        }

        List<Vector3> linePoints = new List<Vector3>();

        
        foreach (Vector2Int grid_pos in path)
        {
            Vector2 world_pos = grid_to_world(grid_pos);
            cam.transform.LookAt(carGoal.transform);
            var clone = Instantiate(pathMarker, new Vector3(world_pos.x, 0.3f, world_pos.y), Quaternion.identity);
            linePoints.Add(new Vector3(world_pos.x, 0.3f, world_pos.y));
        }

        // Draw path line
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        if (path.Count >= 2)
        {
            lineRenderer.positionCount = path.Count;
            lineRenderer.SetPositions(linePoints.ToArray());
        }

        // Draw waypoint
        paths = GameObject.FindGameObjectsWithTag("Marker");
        if (paths != null)
        {
            foreach (GameObject marker in paths)
            {
                marker.transform.localScale = new Vector3(gridWidth, 0.1f, gridWidth);
            }
        }
        

        // Move car along path
        Vector3 direction = new Vector3(0, 0, 0);
        if(path.Count >= 2)
        {
            direction = (new Vector3(path[1].x, 0, path[1].y) - car.getCarPosinGrid()).normalized;
        }

        if (!stop)
        {
            rb.position += direction * (Time.deltaTime * speed);
        }
    }

    public void QuitGame()
    {
        // save any game data here
        #if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void setInitialObstacles()
    {
        GameObject[] scene_obstacles = GameObject.FindGameObjectsWithTag("obstacle");
        if (scene_obstacles != null)
        {
            foreach (GameObject obstacle in scene_obstacles)
            {
                setObstacles(obstacle);
                setUnsafeRegion(obstacle);
            }
        }
    }


    public void UpdateObstacles()
    {
        obstacles.Clear();
        obstacle_objects.Clear();
        setInitialObstacles();

        //Debug.Log("update obstacles 1");
        foreach (GameObject obj in obstacle_objects)
        {
            setObstacles(obj);
        }

        //Debug.Log("update obstacles 2");
        //int gridDimension = predictionVisualizer.getGridDimension();
        GameObject[] blocks = predictionVisualizer.getBlocks();

        for (int i=0; i<predictionVisualizer.gridDimension * predictionVisualizer.gridDimension; i++) 
        {
            //Debug.Log("boolean value is "+ predictionVisualizer.blocks_index[i].ToString());
            if (predictionVisualizer.blocks_index[i]) 
            {
                setObstacles(blocks[i]);
                //Debug.Log("Obstacles " + i + " set!");
            }
        }
    }

    public void setObstacles(GameObject o)
    {
        if (!obstacle_objects.Contains(o))
        {
            obstacle_objects.Add(o);
        }
        
        //obstacle_objects.Add(o);

        // obstacles must be squares for now
        Vector3 position = o.transform.position;
        //Vector3 theta = o.transform.eulerAngles.y;
        Vector3 size = o.transform.localScale;

        // bounding box
        Vector2Int start = world_to_grid(new Vector2(position.x - size.x/2f, position.z - size.z/2f));
        Vector2Int end = world_to_grid(new Vector2(position.x + size.x/2f, position.z + size.z/2f));

        for (int i=start.x; i<end.x; i++) 
        {
            for (int j=start.y; j<end.y; j++) 
            {
                obstacles.Add(new Vector2Int(i, j));
            }
        }
    }

    public void setUnsafeRegion(GameObject o)
    {
        if (!obstacle_objects.Contains(o))
        {
            obstacle_objects.Add(o);
        }

        // obstacles must be squares for now
        Vector3 position = o.transform.position;
        //Vector3 theta = o.transform.eulerAngles.y;
        Vector3 size = o.transform.localScale;

        // bounding box
        int start_x = (int)Math.Floor(position.x - size.x / 2);
        int start_z = (int)Math.Floor(position.z - size.z / 2);
        int end_x = (int)Math.Ceiling(position.x + size.x / 2);
        int end_z = (int)Math.Ceiling(position.z + size.z / 2);

        for (int i = start_x; i < end_x; i++)
        {
            for (int j = start_z; j < end_z; j++)
            {
                Vector2 obstacle = new Vector2((float)i, (float)j);
                for(int p = -safeRegionSize; p <= safeRegionSize; p++)
                {
                    for(int q = -safeRegionSize; q <= safeRegionSize; q++)
                    {
                        Vector2 newPos = new Vector2((float)(i + p), (float)(j + q));
                        if (!unsafeRegion.Contains(newPos))
                        {
                            unsafeRegion.Add(newPos);
                        }
                    }
                }
            }
        }
    }

    public void stopCar()
    {
        stop = !stop;
    }

    //map    
    public bool isObstacle(ref Vector2Int pos)
    {
        return obstacles.Contains(pos);
    }

    public bool inUnsafeRegion(Vector2 pos)
    {
        return unsafeRegion.Contains(pos);
    }

    public Vector2Int getStart()
    {
        return car.current_grid;
    }

    public List<Vector2Int> getGoals()
    {
        return car.goals;
    }

    static public Vector2Int world_to_grid(Vector2 world_pos) {
        int offset = gridNumber / 2;
        return new Vector2Int(Mathf.FloorToInt(world_pos.x / gridWidth) + offset, Mathf.FloorToInt(world_pos.y / gridWidth) + offset);
    }

    public Vector2 grid_to_world(Vector2Int grid_pos) {
        int offset = gridNumber / 2;
        return new Vector2((grid_pos.x - offset + 0.5f) * gridWidth, (grid_pos.y - offset + 0.5f) * gridWidth);

    }
}


public class Car
{
    public GameObject c;
    public Vector2Int current_grid;
    public List<Vector2Int> goals;
    
    public Car(GameObject c, List<Vector2Int> goals_)
    {
    	this.c = c;
        Vector2 car_world = new Vector2(c.transform.position.x, c.transform.position.z);
        current_grid = MazeManager.world_to_grid(car_world);
        goals = goals_;
    }
    
    public void update()
    {
        Vector2 car_world = new Vector2(c.transform.position.x, c.transform.position.z);
        current_grid = MazeManager.world_to_grid(car_world);
        // Debug.Log(current);
    }

    public Vector3 getCarPosinGrid()
    {
        Vector3 pos = new Vector3(current_grid.x, 0, current_grid.y);
        return pos;
    }
}
    

public class Pedestrian
{
    public Vector2 start;
    public Vector2 current;
    public List<Vector2> end;
    public List<Vector2> prediction;
    
    public Pedestrian(Vector3 pos, Vector2 start_, List<Vector2> end_)
    {
        start = start_;
        current = new Vector2(pos.x, pos.z);
        end = end_;
        prediction = new List<Vector2>();
    }    
}