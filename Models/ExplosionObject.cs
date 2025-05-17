namespace TheAdventure.Models;

public class ExplosionObject : TemporaryGameObject
{
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int OwnerId { get; private set; }
    
    public ExplosionObject(SpriteSheet spriteSheet, double lifetimeSeconds, (int X, int Y) position, 
                          int gridX, int gridY, int ownerId) 
        : base(spriteSheet, lifetimeSeconds, position)
    {
        GridX = gridX;
        GridY = gridY;
        OwnerId = ownerId;
        
        // Activate explosion animation
        spriteSheet.ActivateAnimation("Explode");
    }
}
