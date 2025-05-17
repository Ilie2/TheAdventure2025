using Silk.NET.Maths;

namespace TheAdventure.Models.Data;

public enum BlockType
{
    Empty,
    Solid,    // Indestructible blocks
    Brick,    // Destructible blocks
    PowerUp   // Hidden power-up
}

public class BombermanMap
{
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public int TileSize { get; private set; }
    public BlockType[,] Grid { get; private set; }
    
    public BombermanMap(int width, int height, int tileSize)
    {
        GridWidth = width;
        GridHeight = height;
        TileSize = tileSize;
        Grid = new BlockType[width, height];
        
        // Initialize with default empty map
        InitializeDefaultMap();
    }
    
    private void InitializeDefaultMap()
    {
        // Fill with empty spaces
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Grid[x, y] = BlockType.Empty;
            }
        }
        
        // Add solid blocks in a grid pattern (every other block)
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                // Border walls
                if (x == 0 || y == 0 || x == GridWidth - 1 || y == GridHeight - 1)
                {
                    Grid[x, y] = BlockType.Solid;
                    continue;
                }
                
                // Interior solid blocks in a grid pattern
                if (x % 2 == 0 && y % 2 == 0)
                {
                    Grid[x, y] = BlockType.Solid;
                }
            }
        }
        
        // Add destructible blocks randomly (about 40% of remaining spaces)
        Random random = new Random();
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                // Skip solid blocks and player starting positions
                if (Grid[x, y] == BlockType.Solid)
                    continue;
                    
                // Keep player starting positions clear (corners)
                if ((x <= 2 && y <= 2) || // Top-left corner
                    (x >= GridWidth - 3 && y <= 2) || // Top-right corner
                    (x <= 2 && y >= GridHeight - 3) || // Bottom-left corner
                    (x >= GridWidth - 3 && y >= GridHeight - 3)) // Bottom-right corner
                {
                    continue;
                }
                
                // 40% chance to place a brick
                if (random.Next(100) < 40)
                {
                    Grid[x, y] = BlockType.Brick;
                }
            }
        }
    }
    
    public bool IsWalkable(int gridX, int gridY)
    {
        // Check if position is within bounds
        if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight)
            return false;
            
        // Check if the position is empty
        return Grid[gridX, gridY] == BlockType.Empty;
    }
    
    public Vector2D<int> GridToWorld(int gridX, int gridY)
    {
        return new Vector2D<int>(gridX * TileSize + TileSize / 2, gridY * TileSize + TileSize / 2);
    }
    
    public (int X, int Y) WorldToGrid(int worldX, int worldY)
    {
        return (worldX / TileSize, worldY / TileSize);
    }
    
    public void DestroyBlock(int gridX, int gridY)
    {
        if (gridX >= 0 && gridX < GridWidth && gridY >= 0 && gridY < GridHeight)
        {
            if (Grid[gridX, gridY] == BlockType.Brick)
            {
                Grid[gridX, gridY] = BlockType.Empty;
            }
        }
    }
}
