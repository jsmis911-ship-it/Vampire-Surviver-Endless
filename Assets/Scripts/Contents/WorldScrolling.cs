using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldScrolling : MonoBehaviour
{
    public void SetFollowTarget(Transform target) { _playerTransform = target; }
    [SerializeField]
    private Transform _playerTransform;
    private Vector2Int _currentTilePosition = new Vector2Int(0,0);
    [SerializeField ]
    private Vector2Int _playerTilePosition;
    private Vector2Int _onTileGridPlayerPosition;

    [SerializeField]
    private float _tileSize = 25f;
    private GameObject[,] _terrainTiles;


    [SerializeField] 
    private int terrainTileHorizontalCount;
    [SerializeField]
    private int terrainTileVerticalCount;

    [SerializeField]
    private int fieldOfVisionHeight = 3;
    [SerializeField]
    private int fieldOfVisionWidth = 3;

    private void Awake()
    {
        // 不再在 Awake 依赖玩家生成时序，改为在 Update 中延迟获取
        _terrainTiles = new GameObject[terrainTileHorizontalCount, terrainTileVerticalCount];
    }

    private void Update()
    {
        // 玩家尚未生成或已销毁时，延迟重新获取
        if (_playerTransform == null)
        {
            GameObject player = Managers.Game.getPlayer();
            if (player == null)
                return;
            _playerTransform = player.transform;
        }

        _playerTilePosition.x = (int)(_playerTransform.position.x / _tileSize);
        _playerTilePosition.y = (int)(_playerTransform.position.y / _tileSize);

        _playerTilePosition.x -= _playerTransform.position.x < 0 ? 1 : 0;
        _playerTilePosition.y -= _playerTransform.position.y < 0 ? 1 : 0;

        if (_currentTilePosition != _playerTilePosition)
        {
            _currentTilePosition = _playerTilePosition;

            _currentTilePosition.x = CalculatePositionOnAxis(_currentTilePosition.x, true);
            _currentTilePosition.y = CalculatePositionOnAxis(_currentTilePosition.y, false);
            UpdateTileInScreen();
        }
    }

    private void UpdateTileInScreen()
    {
        if (_terrainTiles == null)
            return;

        for(int pov_x= -(fieldOfVisionWidth/2); pov_x <= fieldOfVisionWidth/2; pov_x++)
        {
            for (int pov_y = -(fieldOfVisionHeight/2); pov_y <= fieldOfVisionWidth/2; pov_y++)
            {
                int tileToUpdate_x = CalculatePositionOnAxis(_playerTilePosition.x + pov_x, true);
                int tileToUpdate_y = CalculatePositionOnAxis(_playerTilePosition.y + pov_y, false);

                // 瓦片尚未注册时跳过，避免空引用
                if (tileToUpdate_x < 0 || tileToUpdate_x >= terrainTileHorizontalCount
                    || tileToUpdate_y < 0 || tileToUpdate_y >= terrainTileVerticalCount)
                    continue;

                GameObject tile = _terrainTiles[tileToUpdate_x, tileToUpdate_y];
                if (tile == null)
                    continue;

                tile.transform.position = CalculateTilePosition(
                    _playerTilePosition.x + pov_x,
                    _playerTilePosition.y + pov_y
                    );
            }
        }
    }

    private Vector3 CalculateTilePosition(int x, int y)
    {
        return new Vector3(x * _tileSize, y * _tileSize, 0f);
    }

    private int CalculatePositionOnAxis(float currentValue, bool horizontal)
    {
        if (horizontal)
        {
            if(currentValue >= 0)
                currentValue = currentValue % terrainTileHorizontalCount;
            else
            {
                currentValue += 1;
                currentValue = (terrainTileHorizontalCount - 1) + currentValue % terrainTileHorizontalCount;
            }
                
        }
        else
        {
            if (currentValue >= 0)
                currentValue = currentValue % terrainTileVerticalCount;
            else
            {
                currentValue += 1;
                currentValue = (terrainTileVerticalCount - 1) + currentValue % terrainTileVerticalCount;
            }
                
        }

        return (int)currentValue;
    }

    internal void Add(GameObject tileGameObject, Vector2Int tilePosition)
    {
        _terrainTiles[tilePosition.x, tilePosition.y] = tileGameObject;
    }
}
