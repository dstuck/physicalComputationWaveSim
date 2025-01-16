using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BoatController : MonoBehaviour
{
    #region Private Fields
    [Header("Boat Settings")]
    [SerializeField] private float m_BuoyancyForce = 15f;
    [SerializeField] private float m_WaterDrag = 2f;
    [SerializeField] private float m_MovementSpeed = 5f;
    [SerializeField] private float m_RotationSpeed = 100f;
    
    private Rigidbody2D m_Rigidbody;
    private WaterSurfaceManager m_WaterSurface;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody2D>();
        m_WaterSurface = FindFirstObjectByType<WaterSurfaceManager>();
        
        if (m_WaterSurface == null)
        {
            Debug.LogError($"[{nameof(BoatController)}] No WaterSurfaceManager found in scene!");
        }
    }

    private void FixedUpdate()
    {
        ApplyBuoyancy();
        HandleMovement();
    }
    #endregion

    #region Private Methods
    private void ApplyBuoyancy()
    {
        // Get the water height at the boat's position
        float waterHeight = GetWaterHeight();
        float boatHeight = transform.position.y;
        float boatWidth = GetComponent<Collider2D>().bounds.size.x;

        // Calculate buoyancy points at the bottom corners of the boat
        float leftHeight = GetWaterHeight(transform.position - Vector3.right * boatWidth * 0.5f);
        float rightHeight = GetWaterHeight(transform.position + Vector3.right * boatWidth * 0.5f);
        
        // Calculate the average water height and angle
        float averageHeight = (leftHeight + rightHeight) * 0.5f;
        float waterAngle = Mathf.Atan2(rightHeight - leftHeight, boatWidth) * Mathf.Rad2Deg;

        // Apply forces when boat is near or below water level
        float submersionDepth = averageHeight - boatHeight;
        if (submersionDepth > -1f) // Start applying force slightly above water
        {
            // Calculate buoyancy force based on submersion
            float buoyancyMultiplier = Mathf.Clamp01((submersionDepth + 1f) / 2f);
            Vector2 buoyancyForce = Vector2.up * m_BuoyancyForce * buoyancyMultiplier;
            m_Rigidbody.AddForce(buoyancyForce, ForceMode2D.Force);

            // Apply torque to match water surface angle
            float angleDifference = waterAngle - transform.eulerAngles.z;
            angleDifference = Mathf.DeltaAngle(transform.eulerAngles.z, waterAngle);
            m_Rigidbody.AddTorque(angleDifference * 0.5f);

            // Apply water resistance
            m_Rigidbody.linearDamping = m_WaterDrag * buoyancyMultiplier;
            m_Rigidbody.angularDamping = m_WaterDrag * 0.5f * buoyancyMultiplier;
        }
        else
        {
            // Reset drag when above water
            m_Rigidbody.linearDamping = 0.05f;
            m_Rigidbody.angularDamping = 0.05f;
        }
    }

    private void HandleMovement()
    {
        // Get input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Apply movement force
        Vector2 movement = transform.right * (verticalInput * m_MovementSpeed);
        m_Rigidbody.AddForce(movement, ForceMode2D.Force);

        // Apply rotation
        float rotation = -horizontalInput * m_RotationSpeed * Time.fixedDeltaTime;
        m_Rigidbody.AddTorque(rotation);
    }

    private float GetWaterHeight()
    {
        if (m_WaterSurface == null) return float.MinValue;

        // Get the water height by finding the closest edge collider point
        Vector2[] waterPoints = m_WaterSurface.GetComponent<EdgeCollider2D>().points;
        Vector2 localBoatPos = m_WaterSurface.transform.InverseTransformPoint(transform.position);
        
        // Find the two closest points
        int leftIndex = Mathf.FloorToInt((localBoatPos.x + m_WaterSurface.transform.localScale.x * 5f) / m_WaterSurface.transform.localScale.x * (waterPoints.Length - 1));
        leftIndex = Mathf.Clamp(leftIndex, 0, waterPoints.Length - 2);
        int rightIndex = leftIndex + 1;
        
        // Interpolate between points
        Vector2 leftPoint = waterPoints[leftIndex];
        Vector2 rightPoint = waterPoints[rightIndex];
        float t = (localBoatPos.x - leftPoint.x) / (rightPoint.x - leftPoint.x);
        float interpolatedHeight = Mathf.Lerp(leftPoint.y, rightPoint.y, t);
        
        // Convert back to world space
        return m_WaterSurface.transform.TransformPoint(new Vector3(0, interpolatedHeight, 0)).y;
    }

    // Helper method to get water height at any position
    private float GetWaterHeight(Vector3 _position)
    {
        if (m_WaterSurface == null) return float.MinValue;

        Vector2[] waterPoints = m_WaterSurface.GetComponent<EdgeCollider2D>().points;
        Vector2 localPos = m_WaterSurface.transform.InverseTransformPoint(_position);
        
        int leftIndex = Mathf.FloorToInt((localPos.x + m_WaterSurface.transform.localScale.x * 5f) / m_WaterSurface.transform.localScale.x * (waterPoints.Length - 1));
        leftIndex = Mathf.Clamp(leftIndex, 0, waterPoints.Length - 2);
        int rightIndex = leftIndex + 1;
        
        Vector2 leftPoint = waterPoints[leftIndex];
        Vector2 rightPoint = waterPoints[rightIndex];
        float t = (localPos.x - leftPoint.x) / (rightPoint.x - leftPoint.x);
        float interpolatedHeight = Mathf.Lerp(leftPoint.y, rightPoint.y, t);
        
        return m_WaterSurface.transform.TransformPoint(new Vector3(0, interpolatedHeight, 0)).y;
    }
    #endregion

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw water detection ray
        Gizmos.color = Color.blue;
        Vector3 start = transform.position + Vector3.up * 10f;
        Vector3 end = transform.position + Vector3.down * 10f;
        Gizmos.DrawLine(start, end);
    }
    #endif
} 