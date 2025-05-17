using TheAdventure.Models.Data;

namespace TheAdventure.Models;

public class BombermanPlayer : PlayerObject
{
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int PlayerId { get; private set; }
    
    // Bomberman-specific attributes
    public int BombCount { get; private set; }
    public int MaxBombs { get; private set; }
    public int ExplosionRange { get; private set; }
    public double MoveSpeed { get; private set; }
    public bool IsAlive { get; private set; }
    
    private readonly BombermanMap _map;
    private readonly Action<int, int, int, int, double> _placeBomb;
    private readonly List<int> _activeBombIds = new();
    
    public BombermanPlayer(SpriteSheet spriteSheet, int x, int y, int playerId, BombermanMap map, 
                          Action<int, int, int, int, double> placeBomb) 
        : base(spriteSheet, x, y)
    {
        PlayerId = playerId;
        _map = map;
        _placeBomb = placeBomb;
        
        // Initialize Bomberman-specific attributes
        MaxBombs = 1;
        BombCount = MaxBombs;
        ExplosionRange = 1;
        MoveSpeed = 1.0;
        IsAlive = true;
        
        // Set initial grid position
        (GridX, GridY) = map.WorldToGrid(x, y);
    }
    
    public void UpdateGridPosition()
    {
        // Update the grid position based on the current world position
        (GridX, GridY) = _map.WorldToGrid(Position.X, Position.Y);
    }
    
    public override void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (!IsAlive)
            return;
            
        // Get current grid position
        var currentGridPos = _map.WorldToGrid(Position.X, Position.Y);
        
        // Calculate new position based on input
        var pixelsToMove = (int)(128 * MoveSpeed * (time / 1000.0)); // 128 is the speed value from PlayerObject
        
        // Try to move in the requested direction
        int newX = Position.X;
        int newY = Position.Y;
        
        // Prioritize movement in the dominant direction
        if (Math.Abs(up - down) > Math.Abs(left - right))
        {
            // Vertical movement is dominant
            if (up > down)
            {
                // Try to move up
                int targetY = Position.Y - pixelsToMove;
                var targetGridY = _map.WorldToGrid(Position.X, targetY).Y;
                if (_map.IsWalkable(currentGridPos.X, targetGridY))
                {
                    newY = targetY;
                }
                // We can't align horizontally with our current implementation
            }
            else if (down > up)
            {
                // Try to move down
                int targetY = Position.Y + pixelsToMove;
                var targetGridY = _map.WorldToGrid(Position.X, targetY).Y;
                if (_map.IsWalkable(currentGridPos.X, targetGridY))
                {
                    newY = targetY;
                }
                // We can't align horizontally with our current implementation
            }
        }
        else
        {
            // Horizontal movement is dominant
            if (left > right)
            {
                // Try to move left
                int targetX = Position.X - pixelsToMove;
                var targetGridX = _map.WorldToGrid(targetX, Position.Y).X;
                if (_map.IsWalkable(targetGridX, currentGridPos.Y))
                {
                    newX = targetX;
                }
                // We can align vertically to grid
                AlignVertically(newX, ref newY);
            }
            else if (right > left)
            {
                // Try to move right
                int targetX = Position.X + pixelsToMove;
                var targetGridX = _map.WorldToGrid(targetX, Position.Y).X;
                if (_map.IsWalkable(targetGridX, currentGridPos.Y))
                {
                    newX = targetX;
                }
                // We can align vertically to grid
                AlignVertically(newX, ref newY);
            }
        }
        
        // Update animation state based on movement
        var newState = State.State;
        var newDirection = State.Direction;
        
        if (newX == Position.X && newY == Position.Y)
        {
            if (State.State == PlayerState.Attack)
            {
                if (SpriteSheet.AnimationFinished)
                {
                    newState = PlayerState.Idle;
                }
            }
            else
            {
                newState = PlayerState.Idle;
            }
        }
        else
        {
            newState = PlayerState.Move;
            
            if (newY < Position.Y && newDirection != PlayerStateDirection.Up)
            {
                newDirection = PlayerStateDirection.Up;
            }
            else if (newY > Position.Y && newDirection != PlayerStateDirection.Down)
            {
                newDirection = PlayerStateDirection.Down;
            }
            else if (newX < Position.X && newDirection != PlayerStateDirection.Left)
            {
                newDirection = PlayerStateDirection.Left;
            }
            else if (newX > Position.X && newDirection != PlayerStateDirection.Right)
            {
                newDirection = PlayerStateDirection.Right;
            }
        }
        
        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }
        
        // Update position
        Position = (newX, newY);
        
        // Update grid position
        UpdateGridPosition();
    }
    
    private void AlignVertically(int x, ref int y)
    {
        // Try to align vertically to the center of the grid cell
        int gridY = _map.WorldToGrid(x, y).Y;
        int centerY = gridY * _map.TileSize + _map.TileSize / 2;
        int diff = centerY - y;
        
        // If we're close to the center, snap to it
        if (Math.Abs(diff) < 5)
        {
            y = centerY;
        }
        // Otherwise move towards the center
        else if (Math.Abs(diff) < 20)
        {
            y += Math.Sign(diff) * 2;
        }
    }
    
    public bool PlaceBomb()
    {
        if (!IsAlive || BombCount <= 0)
            return false;
            
        // Check if there's already a bomb at this position
        if (_map.Grid[GridX, GridY] != BlockType.Empty)
            return false;
            
        // Place the bomb
        _placeBomb(GridX, GridY, ExplosionRange, PlayerId, 3.0);
        int bombId = 0; // We'll need to track bombs differently since PlaceBomb no longer returns an ID
        _activeBombIds.Add(bombId);
        BombCount--;
        
        return true;
    }
    
    public void BombExploded(int bombId)
    {
        if (_activeBombIds.Contains(bombId))
        {
            _activeBombIds.Remove(bombId);
            BombCount++;
        }
    }
    
    public void Hit()
    {
        if (!IsAlive)
            return;
            
        IsAlive = false;
        GameOver();
    }
    
    public void AddPowerUp(PowerUpType powerUp)
    {
        switch (powerUp)
        {
            case PowerUpType.ExtraBomb:
                MaxBombs++;
                BombCount++;
                break;
            case PowerUpType.LongerExplosion:
                ExplosionRange++;
                break;
            case PowerUpType.SpeedBoost:
                MoveSpeed += 0.2;
                break;
        }
    }
}

public enum PowerUpType
{
    ExtraBomb,
    LongerExplosion,
    SpeedBoost
}
