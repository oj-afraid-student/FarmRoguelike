// MapSystem.cs - 随机地图生成
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;


public partial class MapSystem : Node
{
    private List<List<RoomData>> _currentFloorMap;
    private Vector2I _currentPosition;
    private int _currentFloor = 1;
    
    public RoomData CurrentRoom => GetRoom(_currentPosition);
    public int CurrentFloor => _currentFloor;

    public void GenerateFloor(int floorNumber)
    {
        _currentFloor = floorNumber;
        _currentFloorMap = new List<List<RoomData>>();
        _currentPosition = new Vector2I(0, 0);
        
        var mapSize = CalculateMapSize(floorNumber);
        var bossPosition = new Vector2I(mapSize.X - 1, mapSize.Y - 1);
        
        // 生成房间网格
        for (int x = 0; x < mapSize.X; x++)
        {
            var row = new List<RoomData>();
            
            for (int y = 0; y < mapSize.Y; y++)
            {
                var position = new Vector2I(x, y);
                var roomType = DetermineRoomType(position, bossPosition, floorNumber);
                
                var room = new RoomData
                {
                    Id = $"room_{x}_{y}_{floorNumber}",
                    Position = position,
                    Type = roomType,
                    IsVisited = false,
                    IsCleared = false,
                    Connections = GenerateConnections(position, mapSize),
                    //RoomData = GenerateRoomSpecificData(roomType, floorNumber)
                };
                
                row.Add(room);
            }
            
            _currentFloorMap.Add(row);
        }
        
        // 设置起始房间
        EnterRoom(_currentPosition);
        
        GD.Print($"第 {floorNumber} 层地图生成完成，大小: {mapSize.X}x{mapSize.Y}");
    }

    private Vector2I CalculateMapSize(int floorNumber)
    {
        // 随着层数增加，地图变大
        var baseSize = 3;
        var extra = Mathf.Min(floorNumber / 3, 2); // 每3层增加1格，最多增加2格
        return new Vector2I(baseSize + extra, baseSize + extra);
    }

    private GameEnums.RoomType DetermineRoomType(Vector2I position, Vector2I bossPosition, int floorNumber)
    {
        // 起点房间
        if (position == new Vector2I(0, 0))
            return GameEnums.RoomType.Rest;
        
        // Boss房间
        if (position == bossPosition)
            return GameEnums.RoomType.Boss;
        
        // 随机分配房间类型
        var rand = new Random();
        var weights = new Dictionary<GameEnums.RoomType, float>
        {
            [GameEnums.RoomType.Combat] = 0.5f,
            [GameEnums.RoomType.Event] = 0.2f,
            [GameEnums.RoomType.Shop] = 0.1f,
            [GameEnums.RoomType.Rest] = 0.1f,
            [GameEnums.RoomType.Farming] = 0.1f
        };
        
        // 根据层数调整权重
        if (floorNumber > 3)
            weights[GameEnums.RoomType.Combat] += 0.1f;
        
        var totalWeight = weights.Values.Sum();
        var randomValue = rand.NextDouble() * totalWeight;
        
        float cumulative = 0;
        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (randomValue <= cumulative)
                return kvp.Key;
        }
        
        return GameEnums.RoomType.Combat;
    }

    public bool EnterRoom(Vector2I position)
    {
        var room = GetRoom(position);
        if (room == null)
        {
            GD.PrintErr($"房间不存在: {position}");
            return false;
        }
        
        _currentPosition = position;
        room.IsVisited = true;
        
        GameRoot.Instance.EventBus.EmitRoomEntered(room);
        
        // 根据房间类型触发事件
        HandleRoomEnter(room);
        
        return true;
    }

    private void HandleRoomEnter(RoomData room)
    {
        switch (room.Type)
        {
            case GameEnums.RoomType.Combat:
                GameRoot.Instance.EventBus.StartCombatRoom(room);
                break;
            case GameEnums.RoomType.Event:
                GameRoot.Instance.EventBus.StartEventRoom(room);
                break;
            case GameEnums.RoomType.Shop:
                GameRoot.Instance.EventBus.StartShopRoom(room);
                break;
            case GameEnums.RoomType.Rest:
                GameRoot.Instance.EventBus.StartRestRoom(room);
                break;
            case GameEnums.RoomType.Boss:
                GameRoot.Instance.EventBus.StartBossRoom(room);
                break;
            case GameEnums.RoomType.Farming:
                GameRoot.Instance.EventBus.StartFarmingRoom(room);
                break;
        }
    }

    public void CompleteCurrentRoom()
    {
        var room = CurrentRoom;
        if (room == null) return;
        
        room.IsCleared = true;
        
        // 如果是Boss房间，完成本层
        if (room.Type == GameEnums.RoomType.Boss)
        {
            CompleteFloor();
        }
    }

    private void CompleteFloor()
    {
        GD.Print($"完成第 {_currentFloor} 层!");
        GameRoot.Instance.EventBus.EmitFloorCompleted(_currentFloor);
        
        // TODO:  给予层数奖励
        //var reward = GenerateFloorReward(_currentFloor);
        //GameRoot.Instance.EventBus.EmitRewardSelected(reward);
    }

    public List<Vector2I> GetAvailableMoves()
    {
        var currentRoom = CurrentRoom;
        if (currentRoom == null)
            return new List<Vector2I>();
        
        return currentRoom.Connections
            .Where(pos => GetRoom(pos) != null)
            .ToList();
    }

    private RoomData GetRoom(Vector2I position)
    {
        if (position.X < 0 || position.Y < 0 || 
            position.X >= _currentFloorMap.Count || 
            position.Y >= _currentFloorMap[0].Count)
            return null;
        
        return _currentFloorMap[position.X][position.Y];
    }

	private List<Vector2I> GenerateConnections(Vector2I position, Vector2I mapSize)
	{
		var connections = new List<Vector2I>();
		
		// 上
		if (position.Y > 0)
			connections.Add(new Vector2I(position.X, position.Y - 1));
		// 下
		if (position.Y < mapSize.Y - 1)
			connections.Add(new Vector2I(position.X, position.Y + 1));
		// 左
		if (position.X > 0)
			connections.Add(new Vector2I(position.X - 1, position.Y));
		// 右
		if (position.X < mapSize.X - 1)
			connections.Add(new Vector2I(position.X + 1, position.Y));
		
		return connections;
	}
	
	
}