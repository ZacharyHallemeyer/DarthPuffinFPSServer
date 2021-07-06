using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    /// <summary>Sends a packet to a client via TCP.</summary>
    /// <param name="_toClient">The client to send the packet the packet to.</param>
    /// <param name="_packet">The packet to send to the client.</param>
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    /// <summary>Sends a packet to a client via UDP.</summary>
    /// <param name="_toClient">The client to send the packet the packet to.</param>
    /// <param name="_packet">The packet to send to the client.</param>
    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    /// <summary>Sends a packet to all clients via TCP.</summary>
    /// <param name="_packet">The packet to send.</param>
    private static void SendTCPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }
    /// <summary>Sends a packet to all clients except one via TCP.</summary>
    /// <param name="_exceptClient">The client to NOT send the data to.</param>
    /// <param name="_packet">The packet to send.</param>
    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
    }

    /// <summary>Sends a packet to all clients via UDP.</summary>
    /// <param name="_packet">The packet to send.</param>
    private static void SendUDPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(_packet);
        }
    }
    /// <summary>Sends a packet to all clients except one via UDP.</summary>
    /// <param name="_exceptClient">The client to NOT send the data to.</param>
    /// <param name="_packet">The packet to send.</param>
    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
    }

    #region Packets
    /// <summary>Sends a welcome message to the given client.</summary>
    /// <param name="_toClient">The client to send the packet to.</param>
    /// <param name="_msg">The message to send.</param>
    public static void Welcome(int _toClient, string _msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(_msg);
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    /// <summary>Tells a client to spawn a player.</summary>
    /// <param name="_toClient">The client that should spawn the player.</param>
    /// <param name="_player">The player to spawn.</param>
    public static void SpawnPlayer(int _toClient, Player _player, string _gunName)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.username);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);
            _packet.Write(_gunName);
            _packet.Write(_player.currentGun.currentAmmo);
            _packet.Write(_player.currentGun.reserveAmmo);
            _packet.Write(_player.maxGrappleTime);
            _packet.Write(_player.maxJetPackTime);

            SendTCPData(_toClient, _packet);
        }
    }

    /// <summary>Sends a player's updated position to all clients.</summary>
    /// <param name="_player">The player whose position to update.</param>
    public static void PlayerPosition(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    /// <summary>Sends a player's updated rotation to all clients except to himself (to avoid overwriting the local player's rotation).</summary>
    /// <param name="_player">The player whose rotation to update.</param>
    public static void PlayerRotation(Player _player, Quaternion orientation)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.localRotation * orientation);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    public static void PlayerDisconnect(int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            _packet.Write(_playerId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerHealth(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerHealth))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.health);

            SendTCPDataToAll(_packet);
        }

        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != _player.id)
                {
                    using (Packet _packet = new Packet((int)ServerPackets.otherPlayerTakenDamage))
                    {
                        _packet.Write(_player.id);
                        _packet.Write(_client.id);

                        SendTCPData(_client.id, _packet);
                    }
                }
            }
        }
    }

    public static void PlayerRespawned(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawned))
        {
            _packet.Write(_player.id);

            SendTCPDataToAll(_packet);
        }
    }

    public static void CreateEnvironment(int _toClient, Vector3 _position, Vector3 _localScale)
    {
        using (Packet _packet = new Packet((int)ServerPackets.createEnvironment))
        {
            _packet.Write(_position);
            _packet.Write(_localScale);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void CreateBoundary(int _toClient, Vector3 _position, float radius)
    {
        using (Packet _packet = new Packet((int)ServerPackets.createBoundary))
        {
            _packet.Write(_position);
            _packet.Write(radius);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerStartGrapple(int _toClient)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerStartGrapple))
        {
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerContinueGrapple(int _toClient, float _currentGrappleTime)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerContinueGrapple))
        {
            _packet.Write(_toClient);
            _packet.Write(_currentGrappleTime);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void OtherPlayerContinueGrapple(int _fromClient, Vector3 _position, Vector3 _grapplePoint)
    {
        foreach(Client _client in Server.clients.Values)
        {
            if(_client.player != null)
            {
                if(_client.id != _fromClient)
                {
                    using (Packet _packet = new Packet((int)ServerPackets.otherPlayerContinueGrapple))
                    {
                        _packet.Write(_fromClient);
                        _packet.Write(_client.id);
                        _packet.Write(_position);
                        _packet.Write(_grapplePoint);

                        SendUDPData(_fromClient, _packet);
                    }
                }
            }
        }
    }

    public static void PlayerStopGrapple(int _toClient)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerStopGrapple))
        {
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != _toClient)
                {
                    using (Packet _packet = new Packet((int)ServerPackets.otherPlayerStopGrapple))
                    {
                        _packet.Write(_toClient);
                        _packet.Write(_client.id);

                        SendUDPData(_toClient, _packet);
                    }
                }
            }
        }
    }

    public static void OtherPlayerSwitchedWeapon(int _fromClient, int _toClient, string _gunName)
    {
        using (Packet _packet = new Packet((int)ServerPackets.otherPlayerSwitchedWeapon))
        {
            _packet.Write(_fromClient);
            _packet.Write(_toClient);
            _packet.Write(_gunName);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerSingleFire(int _toClient, int _currentAmmo, int _reserveAmmo)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerSinglefire))
        {
            _packet.Write(_toClient);
            _packet.Write(_currentAmmo);
            _packet.Write(_reserveAmmo);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerStartAutomaticFire(int _toClient, int _currentAmmo, int _reserveAmmo)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerStartAutomaticFire))
        {
            _packet.Write(_toClient);
            _packet.Write(_currentAmmo);
            _packet.Write(_reserveAmmo);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerContinueAutomaticFire(int _toClient, int _currentAmmo, int _reserveAmmo)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerContinueAutomaticFire))
        {
            _packet.Write(_toClient);
            _packet.Write(_currentAmmo);
            _packet.Write(_reserveAmmo);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerStopAutomaticFire(int _toClient)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerStopAutomaticFire))
        {
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerReload(int _toClient, int _currentAmmo, int _reserveAmmo)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerReload))
        {
            _packet.Write(_toClient);
            _packet.Write(_currentAmmo);
            _packet.Write(_reserveAmmo);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerSwitchWeapon(int _toClient, string _gunName, int _currentAmmo, int _reserveAmmo)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerSwitchWeapon))
        {
            _packet.Write(_toClient);
            _packet.Write(_currentAmmo);
            _packet.Write(_reserveAmmo);
            _packet.Write(_gunName);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerShotLanded(int _toClient, Vector3 _hitPoint)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerShotLanded))
        {
            _packet.Write(_toClient);
            _packet.Write(_hitPoint);

            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerContinueJetPack(int _toClient, float _currentJetPackTime)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerContinueJetPack))
        {
            _packet.Write(_toClient);
            _packet.Write(_currentJetPackTime);

            SendTCPData(_toClient, _packet);
        }
    }
    #endregion
}