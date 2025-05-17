using Silk.NET.Maths;
using TheAdventure.Models.Data;

namespace TheAdventure.Models;

public class BombObject : TemporaryGameObject
{
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int ExplosionRange { get; private set; }
    public int OwnerId { get; private set; }
    public bool HasExploded { get; private set; }
    
    private readonly BombermanMap _map;
    private readonly Action<int, int, int, int> _createExplosion;
    
    public BombObject(SpriteSheet spriteSheet, double lifetimeSeconds, (int X, int Y) position, 
                     int gridX, int gridY, int explosionRange, int ownerId, 
                     BombermanMap map, Action<int, int, int, int> createExplosion) 
        : base(spriteSheet, lifetimeSeconds, position)
    {
        GridX = gridX;
        GridY = gridY;
        ExplosionRange = explosionRange;
        OwnerId = ownerId;
        HasExploded = false;
        _map = map;
        _createExplosion = createExplosion;
        
        // Activate bomb animation
        spriteSheet.ActivateAnimation("Bomb");
    }
    
    public override void Update(double deltaTime)
    {
        base.Update(deltaTime);
        
        // When the bomb is about to expire, trigger the explosion
        if (!HasExploded && TimeRemaining <= 0.1)
        {
            Explode();
        }
    }
    
    public void Explode()
    {
        if (HasExploded)
            return;
            
        HasExploded = true;
        
        // Create center explosion
        _createExplosion(GridX, GridY, ExplosionRange, OwnerId);
        
        // Create explosions in four directions
        PropagateExplosion(1, 0);  // Right
        PropagateExplosion(-1, 0); // Left
        PropagateExplosion(0, 1);  // Down
        PropagateExplosion(0, -1); // Up
    }
    
    private void PropagateExplosion(int dx, int dy)
    {
        for (int i = 1; i <= ExplosionRange; i++)
        {
            int newX = GridX + (dx * i);
            int newY = GridY + (dy * i);
            
            // Stop if we hit the map boundary
            if (newX < 0 || newX >= _map.GridWidth || newY < 0 || newY >= _map.GridHeight)
                break;
                
            // If we hit a solid block, stop the explosion in this direction
            if (_map.Grid[newX, newY] == BlockType.Solid)
                break;
                
            // Create explosion at this position
            _createExplosion(newX, newY, ExplosionRange, OwnerId);
            
            // If we hit a brick, destroy it and stop the explosion in this direction
            if (_map.Grid[newX, newY] == BlockType.Brick)
            {
                _map.DestroyBlock(newX, newY);
                break;
            }
        }
    }
}
