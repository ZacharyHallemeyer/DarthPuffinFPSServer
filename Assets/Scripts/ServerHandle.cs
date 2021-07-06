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
        Vector2 _moveDirection = _packet.ReadVector2();
        Quaternion _rotation = _packet.ReadQuaternion();
        bool _isAnimInProgress = _packet.ReadBool();

        Server.clients[_fromClient].player.SetInput(_moveDirection, _rotation, _isAnimInProgress);
    }

    public static void PlayerJetPackMovement(int _fromClient, Packet _packet)
    {
        Vector3 _direction = _packet.ReadVector3();

        Server.clients[_fromClient].player.JetPackMovement(_direction);
    }

    public static void PlayerMagnetize(int _fromClient, Packet _packet)
    {
        Server.clients[_fromClient].player.PlayerMagnetize();
    }

    public static void PlayerStartGrapple(int _fromClient, Packet _packet)
    {
        Vector3 _direction = _packet.ReadVector3();

        Server.clients[_fromClient].player.StartGrapple(_direction);
    }

    public static void PlayerContinueGrappling(int _fromClient, Packet _packet)
    {
        Vector3 _position = _packet.ReadVector3();
        Vector3 _grapplePoint = _packet.ReadVector3();

        //ServerSend.OtherPlayerContinueGrapple(_fromClient, _position, _grapplePoint);
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

    public static void PlayerUpdateShootDirection(int _fromClient, Packet _packet)
    {
        Vector3 _firePoint = _packet.ReadVector3();
        Vector3 _fireDirection = _packet.ReadVector3();

        Server.clients[_fromClient].player.UpdateShootDirection(_firePoint, _fireDirection);
    }

    public static void PlayerStopShoot(int _fromClient, Packet _packet)
    {
        Server.clients[_fromClient].player.StopShootContoller();
    }

    public static void PlayerReload(int _fromClient, Packet _packet)
    {
        Server.clients[_fromClient].player.Reload();
    }

    public static void PlayerSwitchWeapon(int _fromClient, Packet _packet)
    {
        Server.clients[_fromClient].player.SwitchWeapon();
    }
}