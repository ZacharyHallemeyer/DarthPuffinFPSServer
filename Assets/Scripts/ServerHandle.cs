using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
        if (_fromClient != _clientIdCheck)
        {
            Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }
        Server.clients[_fromClient].SendIntoGame(_username);
    }

    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        bool[] _inputsBool = new bool[_packet.ReadInt()];
        for (int i = 0; i < _inputsBool.Length; i++)
        {
            _inputsBool[i] = _packet.ReadBool();
        }
        Vector2[] _inputsVector2 = new Vector2[_packet.ReadInt()];
        for(int i = 0; i < _inputsVector2.Length; i++)
        {
            _inputsVector2[i] = _packet.ReadVector2();
        }
        Quaternion _rotation = _packet.ReadQuaternion();
        bool _isAnimInProgress = _packet.ReadBool();

        Server.clients[_fromClient].player.SetInput(_inputsBool, _inputsVector2,_rotation, _isAnimInProgress);
    }

    public static void PlayerStartGrapple(int _fromClient, Packet _packet)
    {
        Vector3 _direction = _packet.ReadVector3();

        Server.clients[_fromClient].player.StartGrapple(_direction);
    }

    public static void PlayerStopGrapple(int _fromClient, Packet _packet)
    {
        Server.clients[_fromClient].player.StopGrapple();
    }

    public static void PlayerStartShoot(int _fromClient, Packet _packet)
    {
        Vector3 _firePoint = _packet.ReadVector3();
        Vector3 _fireDirection = _packet.ReadVector3();

        Server.clients[_fromClient].player.ShootController(_firePoint, _fireDirection);
    }

    public static void PlayerStopShoot(int _fromClient, Packet _packet)
    {
        Server.clients[_fromClient].player.StopShootContoller();
    }
}