using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using static MAVLink;

public enum CUSTOM_MODE 
{
    GUIDED = 4 // Для ArduPilot
}
public class MAVlinkResever: MonoBehaviour
{
    [Header("UDP Settings")]
    public int localPort = 14551; // Порт для приёма MAVLink
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;
    private MavlinkParse mavlinkParser; // Парсер MAVLink


    [Header("MAVLink Data")]
    public float roll;    // Крен (радианы)
    public float pitch;   // Тангаж (радианы)
    public float yaw;     // Рысканье (радианы)
    public float altitude; // Высота (метры)
    public Vector3 position; // GPS-позиция (latitude, longitude, altitude)

    public float[] motorRpm = new float[4];
    public Vector3 posxyz;
    public Vector3 velocity;
    private byte[] buffer = new byte[1];
    private double metersPerDegreeLon = 111111.0 * Math.Cos(-35.3632621 * Math.PI / 180.0);
    public event Action<Vector3> OnPosUpdated;

    void Start()
    {
        mavlinkParser = new MavlinkParse();
        StartUDPListener();
    }

    void OnDestroy()
    {
        StopUDPListener();
    }

    private void StartUDPListener()
    {
        udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, localPort));
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"MAVLink UDP-приёмник запущен на порту {localPort}");
    }

    private void StopUDPListener()
    {
        isRunning = false;
        udpClient?.Close();
        receiveThread?.Abort();
        Debug.Log("MAVLink UDP-приёмник остановлен");
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning)
        {
            try
            {
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                MAVLinkMessage msg = mavlinkParser.ReadPacket(new System.IO.MemoryStream(receivedBytes));
                ProcessMavlinkMessage(msg);
                if (buffer.Length>1)
                {
                    udpClient.Send(buffer, buffer.Length, remoteEndPoint);
                    buffer = new byte[1];
                    Thread.Sleep(500);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка приёма MAVLink: {e.Message}");
            }
        }
    }

    public void SetGuidedMode()
    {
        var packet = new mavlink_set_mode_t
        {
            target_system = 1,
            base_mode = (byte)129,
            custom_mode = (uint)4
        };
        while (buffer.Length>1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.SET_MODE,
            packet
        );
        Debug.Log($"Режим GUIDED активирован");
    }

    public void ArmDrone()
    {
        // Создание сообщения CommandLong
        var packet = new mavlink_command_long_t
        {
            command = (ushort)400,
            target_system = 1,    // ID системы автопилота
            target_component = 1, // ID компонента
            param1 = 1,       
            param2 = 0,
            param3 = 0,
            param4 = 0,
            param5 = 0,
            param6 = 0,
            param7 = 0,
            confirmation = 0
        };
        while (buffer.Length>1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.COMMAND_LONG,
            packet
        );
    }

    public void Takeoff()
    {
        // Создание сообщения CommandLong
        var packet = new mavlink_command_long_t
        {
            command = (ushort)22,
            target_system = 1,    // ID системы автопилота
            target_component = 1, // ID компонента
            param1 = 0,       
            param2 = 0,
            param3 = 0,
            param4 = 0,
            param5 = 0,
            param6 = 0,
            param7 = 10,
            confirmation = 0
        };
        while (buffer.Length>1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.COMMAND_LONG,
            packet
        );
    }

    public void DoRepos(Vector3 point)
    {
        ClearMission();
        CreateAndUploadMission(new List<Vector3> (new Vector3[]{ point }), false);
        StartMission();
    }

    public void Land()
    {
        // Создание сообщения CommandLong
        var packet = new mavlink_command_long_t
        {
            command = (ushort)21,
            target_system = 1,    // ID системы автопилота
            target_component = 1, // ID компонента
            param1 = 0,       
            param2 = 0,
            param3 = 0,
            param4 = 0,
            param5 = (float)(-35.3632621),
            param6 = (float)(149.1652373),
            param7 = 0,
            confirmation = 0
        };
        while (buffer.Length>1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.COMMAND_LONG,
            packet
        );
    }
    
    private List<mavlink_mission_item_int_t> CreateSimpleMission(List<Vector3> point, bool full=true)
    {
        var missionItems = new List<mavlink_mission_item_int_t>();

        if (full)
        {
            missionItems.Add(new mavlink_mission_item_int_t
            {
                command = (ushort)22, // 22
                autocontinue = 1,
                z = 10, // Высота взлёта
                frame = (byte)3
            });
        }
        missionItems.Add(new mavlink_mission_item_int_t
        {
            command = (ushort)22, // 22
            autocontinue = 1,
            z = 10, // Высота взлёта
            frame = (byte)3
        });
        for (int i = 0; i < point.Count; i++)
        {
            missionItems.Add(new mavlink_mission_item_int_t
            {
                command = (ushort)16, // MAV_CMD_NAV_WAYPOINT
                frame = (byte)6, // MAV_FRAME_LOCAL_NED
                autocontinue = 1,
                x = (int)((-35.3632621 + point[i].z/111111.0)* 1e7), // Умножаем на 100 для точности (см)
                y = (int)((149.1652373 + point[i].x/metersPerDegreeLon)* 1e7),  // Умножаем на 100 для точности (см)
                z = (float)point[i].y  // Высота в метрах 
            });
            Debug.Log($"Создана точка {i}");
        }
        if (full)
        {
            missionItems.Add(new mavlink_mission_item_int_t
            {
                command = (ushort)21, // 21
                autocontinue = 1,
                frame = (byte)3
            });
        }
        return missionItems;
    }

    private void CreateAndUploadMission(List<Vector3> point, bool full=true)
    {
        var missionItems = CreateSimpleMission(point, full);
        ushort count = (ushort)missionItems.Count;

        // 1. Инициализация загрузки миссии
        SendMissionCount(count);

        // 2. Отправка всех элементов миссии
        for (ushort seq = 0; seq < count; seq++)
        {
            SendMissionItem(missionItems[seq], seq, count);
        }

        // 3. Подтверждение загрузки
        SendMissionAck();
    }

    private void SendMissionAck()
    {
        var packet = new mavlink_mission_ack_t
        {
            target_system = 1,
            target_component = 1,
            type = (byte)1
        };
        while (buffer.Length>1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.MISSION_ACK,
            packet
        );
    }

    private void SendMissionCount(ushort count)
    {
        var packet = new mavlink_mission_count_t
        {
            target_system = 1,
            target_component = 1,
            count = count
        };
        while (buffer.Length>1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.MISSION_COUNT,
            packet
        );
        Debug.Log($"Отправлено количество элементов миссии: {count}");
    }

    private void SendMissionItem(mavlink_mission_item_int_t item, ushort seq, ushort count)
    {
        var packet = new mavlink_mission_item_int_t
        {
            target_system = 1,
            target_component = 1,
            seq = seq,
            frame = item.frame,
            command = item.command,
            current = (byte)((seq == 0) ? 1 : 0), // Первая точка - текущая
            autocontinue = 1,
            param1 = item.param1,
            param2 = item.param2,
            param3 = item.param3,
            param4 = item.param4,
            x = item.x,
            y = item.y,
            z = item.z
        };
        while (buffer.Length>1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.MISSION_ITEM_INT,
            packet
        );
        Debug.Log($"Отправлен элемент миссии {seq + 1}/{count}: Команда={packet.command}");
    }

    public void StartMission()
    {
        var packet = new mavlink_command_long_t
        {
            command = (ushort)300, // MAV_CMD_MISSION_START
            target_system = 1,
            target_component = 1,
            confirmation = 0
        };
    
        while (buffer.Length > 1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.COMMAND_LONG,
            packet
        );
        Debug.Log("Команда запуска миссии отправлена");
    }

    public void ClearMission()
    {
        var packet = new mavlink_command_long_t
        {
            command = (ushort)45, // MAV_CMD_MISSION_START
            target_system = 1,
            target_component = 1,
            confirmation = 0
        };
    
        while (buffer.Length > 1)
        {
            Thread.Sleep(300);
        }
        buffer = mavlinkParser.GenerateMAVLinkPacket20(
            MAVLINK_MSG_ID.COMMAND_LONG,
            packet
        );
        Debug.Log("Команда очистки миссии отправлена");
    }

    private void ProcessMavlinkMessage(MAVLinkMessage msg)
    {
        switch (msg.msgid)
        {
            case (uint)MAVLINK_MSG_ID.ATTITUDE:
                var attitude = (mavlink_attitude_t)msg.data;
                roll = attitude.roll;
                pitch = attitude.pitch;
                yaw = attitude.yaw;
                //Debug.Log($"Attitude: Roll={roll}, Pitch={pitch}, Yaw={yaw}");
                break;

            case (uint)MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                var gps = (mavlink_global_position_int_t)msg.data;
                altitude = gps.alt / 1000f; // Из мм в метры
                position = new Vector3(
                    gps.lat / 1e7f,  // Широта (deg)
                    gps.lon / 1e7f,  // Долгота (deg)
                    altitude
                );
                //Debug.Log($"GPS: Lat={position.x}, Lon={position.y}, Alt={altitude}m");
                break;

            case (uint)MAVLINK_MSG_ID.HEARTBEAT:
                var heartbeat = (mavlink_heartbeat_t)msg.data;
                Debug.Log($"Heartbeat: Type={(MAV_TYPE)heartbeat.type}, Mode={(MAV_MODE)heartbeat.base_mode}");
                break;

            case (uint)36: 
                var escTelemetry = (mavlink_servo_output_raw_t)msg.data;
                // Обновите данные для 4 моторов (зависит от реализации автопилота)
                motorRpm[0] = (escTelemetry.servo1_raw-1100)/900.0f; // Motor 1
                motorRpm[1] = (escTelemetry.servo2_raw-1100)/900.0f; // Motor 2
                motorRpm[2] = (escTelemetry.servo3_raw-1100)/900.0f; // Motor 3
                motorRpm[3] = (escTelemetry.servo4_raw-1100)/900.0f; // Motor 4
                //Debug.Log($"Motor RPM: {motorRpm[0]}, {motorRpm[1]}, {motorRpm[2]}, {motorRpm[3]}");
                break;

            case (uint)32: 
                var pos = (mavlink_local_position_ned_t)msg.data;
                // Обновите данные для 4 моторов (зависит от реализации автопилота)
                posxyz = new Vector3(
                    pos.y,  
                    -pos.z,  
                    pos.x
                );
                OnPosUpdated?.Invoke(posxyz);
                velocity = new Vector3(
                    1*pos.vy,   // скорость на север (м/с)
                    -1*pos.vz,  // скорость вниз (vz) -> преобразуем в скорость вверх (инвертируем)
                    1*pos.vx    // скорость на восток (м/с)
                );
                //Debug.Log($"x: {pos.y}, y: {-pos.z}, z: {pos.x}");
                break;

            case (uint)116: 
                var test = (mavlink_scaled_imu2_t)msg.data;
                //Debug.Log($"{test.xmag}, {test.zmag}, {test.ymag}");
                break;

            case (uint)136:
                var test2 = (mavlink_terrain_report_t)msg.data;
                //Debug.Log($"{test2.terrain_height}");
                break;

            case (uint)MAVLINK_MSG_ID.COMMAND_ACK:
                var ack = (mavlink_command_ack_t)msg.data;
                Debug.Log($"Команда {ack.command} подтверждена: {ack.result}");
                break;

            default:
                //Debug.Log($"id {msg.msgid} {msg.data}");
                break;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            SetGuidedMode();
            ArmDrone();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Takeoff();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            ClearMission();
            CreateAndUploadMission(new List<Vector3> (new Vector3[]{ new Vector3(
                    10,
                    10,
                    0
                ),
                new Vector3(
                    0,
                    10,
                    0
                )}));
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartMission();
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            DoRepos(new Vector3(
                10,
                10,
                15
            ));
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            DoRepos(new Vector3(
                0,
                10,
                0
            ));
        }
        if (Input.GetKeyDown(KeyCode.L))
        {  
            Land();
        }
    }
}
