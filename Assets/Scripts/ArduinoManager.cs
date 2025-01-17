using UnityEngine;
using System.IO.Ports;
using System.Linq;
using System.Collections.Generic;

public class ArduinoManager : MonoBehaviour
{
    #region Private Fields
    [Header("Serial Settings")]
    [SerializeField] private string m_PortName = "/dev/tty.usbmodem12201";
    [SerializeField] private int m_BaudRate = 9600;

    [Header("Sensor Settings")]
    [SerializeField] private float m_MinDistance = 0f;
    [SerializeField] private float m_MaxDistance = 50f;
    [SerializeField] private int m_SmoothingFrames = 5;
    [SerializeField] private float m_ResponseSpeed = 5f;
    [SerializeField] private float m_KeyboardControlSpeed = 30f;

    private SerialPort m_Stream;
    private bool m_IsConnected = false;
    private Queue<float> m_SmoothingBuffer;
    private float m_TargetDistance;
    private float m_KeyboardDistance = 25f; // Start at middle range
    
    [SerializeField] public float m_SensorDistance;

    #endregion

    private void Start()
    {
        m_SmoothingBuffer = new Queue<float>();
        InitializeSerialConnection();
    }

    private void Update()
    {
        if (!m_IsConnected)
        {
            // Keyboard control when no Arduino connected
            if (Input.GetKey(KeyCode.UpArrow))
            {
                m_KeyboardDistance = Mathf.Min(m_KeyboardDistance + m_KeyboardControlSpeed * Time.deltaTime, m_MaxDistance);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                m_KeyboardDistance = Mathf.Max(m_KeyboardDistance - m_KeyboardControlSpeed * Time.deltaTime, m_MinDistance);
            }
            
            // Use keyboard value as sensor input
            m_TargetDistance = m_KeyboardDistance;
        }

        // Smoothly interpolate to target distance
        m_SensorDistance = Mathf.Lerp(m_SensorDistance, m_TargetDistance, Time.deltaTime * m_ResponseSpeed);
    }

    private void InitializeSerialConnection()
    {
        string[] availablePorts = SerialPort.GetPortNames();
        Debug.Log($"Available ports: {string.Join(", ", availablePorts)}");

        string arduinoPort = availablePorts.FirstOrDefault(p => p == m_PortName);
        
        if (string.IsNullOrEmpty(arduinoPort))
        {
            Debug.LogError($"[{nameof(ArduinoManager)}] Could not find Arduino on port {m_PortName}. Available ports: {string.Join(", ", availablePorts)}");
            return;
        }

        try
        {
            m_Stream = new SerialPort(arduinoPort, m_BaudRate)
            {
                DtrEnable = true,
                RtsEnable = true,
                ReadTimeout = 2
            };

            m_Stream.Open();
            m_IsConnected = true;
            Debug.Log($"[{nameof(ArduinoManager)}] Connected to Arduino on port {arduinoPort}");
            
            InvokeRepeating(nameof(ReadSensor), 0.2f, 0.02f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{nameof(ArduinoManager)}] Error connecting to Arduino: {e.Message}");
            m_IsConnected = false;
        }
    }

    private void ReadSensor()
    {
        if (!m_IsConnected) return;

        try 
        {
            string sensorData = m_Stream.ReadLine();
            if (float.TryParse(sensorData, out float rawDistance))
            {
                // Clamp and normalize the raw distance
                float clampedDistance = Mathf.Clamp(rawDistance, m_MinDistance, m_MaxDistance);

                // Add to smoothing buffer
                m_SmoothingBuffer.Enqueue(clampedDistance);
                if (m_SmoothingBuffer.Count > m_SmoothingFrames)
                {
                    m_SmoothingBuffer.Dequeue();
                }

                // Calculate smoothed average
                m_TargetDistance = m_SmoothingBuffer.Average();
            }
        }
        catch (System.Exception e) 
        {
            Debug.LogWarning($"[{nameof(ArduinoManager)}] No reading: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (m_Stream != null && m_Stream.IsOpen)
        {
            m_Stream.Close();
            m_IsConnected = false;
        }
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        string[] availablePorts = SerialPort.GetPortNames();
        Debug.Log($"[{nameof(ArduinoManager)}] Available ports: {string.Join(", ", availablePorts)}");
    }
    #endif
}
