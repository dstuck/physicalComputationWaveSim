using UnityEngine;

public class WaterSurfaceManager : MonoBehaviour
{
    #region Private Fields
    [Header("Grid Settings")]
    [SerializeField] private Vector2 m_WaterSize = new Vector2(10f, 10f);
    [SerializeField] private int m_GridDensity = 20; // vertices per unit
    [SerializeField] private float m_CellSize = 0.5f;

    [Header("Wave Settings")]
    [SerializeField, Range(0f, 5f)] private float m_WaveHeight = 1f;
    [SerializeField, Range(0f, 5f)] private float m_WaveSpeed = 1f;
    [SerializeField, Range(0f, 5f)] private float m_WaveLength = 2f;

    private Mesh m_Mesh;
    private Vector3[] m_BaseVertices;
    private Vector3[] m_Vertices;
    private EdgeCollider2D m_EdgeCollider;
    private MeshFilter m_MeshFilter;
    private MeshRenderer m_MeshRenderer;
    private int m_GridSizeX;
    private int m_GridSizeZ;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Ensure all required components exist
        m_MeshFilter = GetComponent<MeshFilter>();
        if (m_MeshFilter == null)
        {
            m_MeshFilter = gameObject.AddComponent<MeshFilter>();
        }

        m_MeshRenderer = GetComponent<MeshRenderer>();
        if (m_MeshRenderer == null)
        {
            m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            // Create and assign a default material
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);
            m_MeshRenderer.material = material;
        }

        m_EdgeCollider = GetComponent<EdgeCollider2D>();
        if (m_EdgeCollider == null)
        {
            m_EdgeCollider = gameObject.AddComponent<EdgeCollider2D>();
        }

        // Remove the MeshCollider component if it exists
        if (TryGetComponent<MeshCollider>(out var meshCollider))
        {
            DestroyImmediate(meshCollider);
        }
    }

    private void Start()
    {
        InitializeWaterMesh();
    }

    private void Update()
    {
        UpdateWaveMotion();
    }
    #endregion

    #region Private Methods
    private void InitializeWaterMesh()
    {
        // Calculate grid sizes based on desired water size and cell size
        m_GridSizeX = Mathf.CeilToInt(m_WaterSize.x / m_CellSize);
        m_GridSizeZ = Mathf.CeilToInt(m_WaterSize.y / m_CellSize);

        // Create mesh
        m_Mesh = new Mesh();
        m_Mesh.name = "WaterMesh";
        m_MeshFilter.mesh = m_Mesh;

        // Generate vertices and triangles
        CreateMeshGrid();
        
        // Store base vertices for wave calculation
        m_BaseVertices = m_Mesh.vertices;
        m_Vertices = new Vector3[m_BaseVertices.Length];
        
        Debug.Log($"[{nameof(WaterSurfaceManager)}] Mesh created with {m_BaseVertices.Length} vertices");
    }

    private void CreateMeshGrid()
    {
        // Create vertices
        Vector3[] vertices = new Vector3[(m_GridSizeX + 1) * (m_GridSizeZ + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        
        // Calculate the offset to center the mesh in XY plane
        Vector3 offset = new Vector3(-m_WaterSize.x * 0.5f, -m_WaterSize.y * 0.5f, 0f);
        
        for (int i = 0; i <= m_GridSizeZ; i++)
        {
            for (int j = 0; j <= m_GridSizeX; j++)
            {
                int index = i * (m_GridSizeX + 1) + j;
                // Place vertices in XY plane instead of XZ
                vertices[index] = new Vector3(j * m_CellSize, i * m_CellSize, 0) + offset;
                uvs[index] = new Vector2((float)j / m_GridSizeX, (float)i / m_GridSizeZ);
            }
        }

        // Create triangles
        int[] triangles = new int[m_GridSizeX * m_GridSizeZ * 6];
        int triangleIndex = 0;
        
        for (int i = 0; i < m_GridSizeZ; i++)
        {
            for (int j = 0; j < m_GridSizeX; j++)
            {
                int vertexIndex = i * (m_GridSizeX + 1) + j;
                
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + m_GridSizeX + 1;
                triangles[triangleIndex + 2] = vertexIndex + 1;
                triangles[triangleIndex + 3] = vertexIndex + 1;
                triangles[triangleIndex + 4] = vertexIndex + m_GridSizeX + 1;
                triangles[triangleIndex + 5] = vertexIndex + m_GridSizeX + 2;
                
                triangleIndex += 6;
            }
        }

        // Apply to mesh
        m_Mesh.vertices = vertices;
        m_Mesh.triangles = triangles;
        m_Mesh.uv = uvs;
        m_Mesh.RecalculateNormals();

        Debug.Log($"[{nameof(WaterSurfaceManager)}] Created mesh grid: {m_GridSizeX}x{m_GridSizeZ} " +
                  $"with {vertices.Length} vertices and {triangles.Length/3} triangles");
    }

    private void UpdateWaveMotion()
    {
        for (int i = 0; i < m_BaseVertices.Length; i++)
        {
            Vector3 vertex = m_BaseVertices[i];
            
            float x = vertex.x;
            float time = Time.time * m_WaveSpeed;
            float wave = m_WaveHeight * Mathf.Sin(x / m_WaveLength + time);
            
            m_Vertices[i] = new Vector3(vertex.x, vertex.y + wave, vertex.z);
        }

        m_Mesh.vertices = m_Vertices;
        m_Mesh.RecalculateNormals();
        
        // Update edge collider points
        UpdateEdgeCollider();
    }

    private void UpdateEdgeCollider()
    {
        // Create points for the edge collider along the top of the water
        Vector2[] edgePoints = new Vector2[m_GridSizeX + 1];
        
        for (int i = 0; i <= m_GridSizeX; i++)
        {
            // Get the vertex at the top edge of the water (last row)
            // Multiply by (GridSizeX + 1) to get to the last row
            int topRowIndex = m_GridSizeZ * (m_GridSizeX + 1) + i;
            Vector3 vertex = m_Vertices[topRowIndex];
            edgePoints[i] = new Vector2(vertex.x, vertex.y);
        }
        
        m_EdgeCollider.points = edgePoints;
    }
    #endregion

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw water boundary
        Gizmos.color = Color.blue;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(m_WaterSize.x, m_WaterSize.y, 0.1f);
        Gizmos.DrawWireCube(center, size);

        // Draw mesh vertices and triangles if mesh exists
        if (m_Mesh != null)
        {
            Gizmos.color = Color.yellow;
            Vector3[] vertices = m_Mesh.vertices;
            int[] triangles = m_Mesh.triangles;

            // Draw vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                Gizmos.DrawSphere(worldPos, 0.1f);
            }

            // Draw triangle edges
            Gizmos.color = Color.green;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v3 = transform.TransformPoint(vertices[triangles[i + 2]]);

                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v1);
            }
        }
    }
    #endif
} 