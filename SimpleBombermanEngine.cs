using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

// A simplified version of the Bomberman engine to get something working
public class SimpleBombermanEngine : Engine
{
    private const int GRID_SIZE = 48; // Size of each grid cell in pixels
    private const int GRID_WIDTH = 15;
    private const int GRID_HEIGHT = 13;
    
    // Simple grid representation: 0 = empty, 1 = solid block, 2 = destructible block
    private int[,] _grid = new int[GRID_WIDTH, GRID_HEIGHT];
    
    public SimpleBombermanEngine(GameRenderer renderer, Input input) : base(renderer, input)
    {
        InitializeGrid();
    }
    
    private void InitializeGrid()
    {
        // Initialize with empty spaces
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                _grid[x, y] = 0; // Empty
            }
        }
        
        // Add solid blocks in a grid pattern (every other block)
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                // Border walls
                if (x == 0 || y == 0 || x == GRID_WIDTH - 1 || y == GRID_HEIGHT - 1)
                {
                    _grid[x, y] = 1; // Solid
                    continue;
                }
                
                // Interior solid blocks in a grid pattern
                if (x % 2 == 0 && y % 2 == 0)
                {
                    _grid[x, y] = 1; // Solid
                }
            }
        }
        
        // Add destructible blocks randomly (about 80% of remaining spaces to make them more visible)
        Random random = new Random();
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                // Skip solid blocks and player starting positions
                if (_grid[x, y] == 1)
                    continue;
                    
                // Keep player starting positions clear (corners)
                if ((x <= 2 && y <= 2) || // Top-left corner
                    (x >= GRID_WIDTH - 3 && y <= 2) || // Top-right corner
                    (x <= 2 && y >= GRID_HEIGHT - 3) || // Bottom-left corner
                    (x >= GRID_WIDTH - 3 && y >= GRID_HEIGHT - 3)) // Bottom-right corner
                {
                    continue;
                }
                
                // 80% chance to place a brick (increased from 60%)
                if (random.Next(100) < 80)
                {
                    _grid[x, y] = 2; // Destructible
                }
            }
        }
        
        // Debug: Print initial grid state
        Console.WriteLine("Initial Grid State:");
        for (int y = 0; y < GRID_HEIGHT; y++)
        {
            string row = "";
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                row += _grid[x, y] + " ";
            }
            Console.WriteLine(row);
        }
    }
    
    public override void SetupWorld()
    {
        // Create player at starting position (top-left corner)
        _player = new PlayerObject(SpriteSheet.Load(Renderer, "Player.json", "Assets"), 
                                  GRID_SIZE + GRID_SIZE/2, GRID_SIZE + GRID_SIZE/2);
        
        // Set world bounds
        Renderer.SetWorldBounds(new Rectangle<int>(0, 0, 
            GRID_WIDTH * GRID_SIZE,
            GRID_HEIGHT * GRID_SIZE));
    }
    
    // Bomb and explosion tracking with a simpler approach
    private class Bomb
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public double TimeRemaining { get; set; } // Seconds until explosion
        
        public Bomb(int x, int y, double time)
        {
            GridX = x;
            GridY = y;
            TimeRemaining = time;
        }
    }
    
    private class Explosion
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public double TimeRemaining { get; set; } // Seconds until disappearing
        
        public Explosion(int x, int y, double time)
        {
            GridX = x;
            GridY = y;
            TimeRemaining = time;
        }
    }
    
    private List<Bomb> _bombs = new List<Bomb>();
    private List<Explosion> _explosions = new List<Explosion>();
    private bool _bombKeyWasPressed = false; // Track if B key was already pressed
    
    public override void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var deltaTime = (currentTime - LastUpdate).TotalSeconds; // Use seconds for easier timing
        LastUpdate = currentTime;
        
        Console.WriteLine($"Active bombs: {_bombs.Count}, Active explosions: {_explosions.Count}");
        
        if (_player == null)
        {
            return;
        }
        
        // Process player input
        double up = Input.IsUpPressed() ? 1.0 : 0.0;
        double down = Input.IsDownPressed() ? 1.0 : 0.0;
        double left = Input.IsLeftPressed() ? 1.0 : 0.0;
        double right = Input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = Input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool bombKeyPressed = Input.IsKeyBPressed();
        
        // Store current position
        var oldX = _player.Position.X;
        var oldY = _player.Position.Y;
        
        // Update player position
        _player.UpdatePosition(up, down, left, right, GRID_SIZE, GRID_SIZE, deltaTime * 1000); // Convert back to ms for player update
        
        // Check for collisions with blocks
        int gridX = _player.Position.X / GRID_SIZE;
        int gridY = _player.Position.Y / GRID_SIZE;
        
        // If we hit a block, revert to old position
        if (_grid[gridX, gridY] != 0)
        {
            _player.Position = (oldX, oldY);
            gridX = oldX / GRID_SIZE;
            gridY = oldY / GRID_SIZE;
        }
        
        // Handle attack
        if (isAttacking)
        {
            _player.Attack();
        }
        
        // Handle bomb placement with key press and release detection
        if (bombKeyPressed && !_bombKeyWasPressed)
        {
            // Get grid position for bomb
            int bombGridX = gridX;
            int bombGridY = gridY;
            
            // Only place bomb if the cell is empty and there's no bomb already there
            bool cellHasBomb = _bombs.Any(b => b.GridX == bombGridX && b.GridY == bombGridY);
            
            if (_grid[bombGridX, bombGridY] == 0 && !cellHasBomb)
            {
                // Create a new bomb
                _bombs.Add(new Bomb(bombGridX, bombGridY, 2.0f)); // 2 second fuse
                Console.WriteLine($"Bomb placed at grid position: {bombGridX}, {bombGridY}");
            }
        }
        _bombKeyWasPressed = bombKeyPressed;
        
        // Update bombs
        for (int i = _bombs.Count - 1; i >= 0; i--)
        {
            _bombs[i].TimeRemaining -= deltaTime;
            
            // If bomb timer is up, explode it
            if (_bombs[i].TimeRemaining <= 0)
            {
                Console.WriteLine($"Bomb timer expired at {_bombs[i].GridX}, {_bombs[i].GridY}");
                ExplodeBomb(_bombs[i].GridX, _bombs[i].GridY);
                _bombs.RemoveAt(i);
            }
        }
        
        // Update explosions
        for (int i = _explosions.Count - 1; i >= 0; i--)
        {
            _explosions[i].TimeRemaining -= deltaTime;
            
            // Remove expired explosions
            if (_explosions[i].TimeRemaining <= 0)
            {
                _explosions.RemoveAt(i);
            }
        }
        
        // Check if player is hit by any explosion
        foreach (var explosion in _explosions)
        {
            if (gridX == explosion.GridX && gridY == explosion.GridY)
            {
                _player.GameOver();
                break;
            }
        }
    }
    
    public override void RenderFrame()
    {
        Renderer.SetDrawColor(0, 0, 0, 255);
        Renderer.ClearScreen();
        
        var playerPosition = _player!.Position;
        Renderer.CameraLookAt(playerPosition.X, playerPosition.Y);
        
        // Render the grid
        RenderGrid();
        
        // Render active bombs
        RenderBombs();
        
        // Render active explosions
        RenderExplosions();
        
        // Render game objects
        RenderAllObjects();
        
        Renderer.PresentFrame();
    }
    
    private void RenderGrid()
    {
        // Use only valid existing textures but with different colors to make them distinct
        int floorTexture = Renderer.LoadTexture("Assets/grass_003.png", out _);
        int solidBlockTexture = Renderer.LoadTexture("Assets/grass_001.png", out _);
        int brickBlockTexture = Renderer.LoadTexture("Assets/grass_002.png", out _);
        
        // Set different colors for different block types
        Renderer.SetTextureColorMod(solidBlockTexture, 50, 50, 150); // Dark blue for solid blocks
        Renderer.SetTextureColorMod(brickBlockTexture, 150, 50, 50); // Dark red for destructible blocks
        
        // Debug: Print grid state
        Console.WriteLine("Current Grid State:");
        for (int y = 0; y < 5; y++) // Just print the first 5 rows to avoid spam
        {
            string row = "";
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                row += _grid[x, y] + " ";
            }
            Console.WriteLine(row);
        }
        
        // Render the grid
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                int textureId;
                
                // Select texture based on block type
                switch (_grid[x, y])
                {
                    case 1: // Solid
                        textureId = solidBlockTexture;
                        break;
                    case 2: // Destructible
                        textureId = brickBlockTexture;
                        break;
                    default: // Empty
                        textureId = floorTexture;
                        break;
                }
                
                // Render the block
                var sourceRect = new Rectangle<int>(0, 0, GRID_SIZE, GRID_SIZE);
                var destRect = new Rectangle<int>(x * GRID_SIZE, y * GRID_SIZE, GRID_SIZE, GRID_SIZE);
                Renderer.RenderTexture(textureId, sourceRect, destRect);
            }
        }
    }
    
    private void ExplodeBomb(int gridX, int gridY)
    {
        Console.WriteLine($"Exploding bomb at grid position: {gridX}, {gridY}");
        
        // Add explosion at bomb position
        _explosions.Add(new Explosion(gridX, gridY, 0.5)); // 0.5 second explosion duration
        
        // Explode in four directions
        ExplodeDirection(gridX, gridY, 1, 0, 3);  // Right
        ExplodeDirection(gridX, gridY, -1, 0, 3); // Left
        ExplodeDirection(gridX, gridY, 0, 1, 3);  // Down
        ExplodeDirection(gridX, gridY, 0, -1, 3); // Up
        
        // Print the grid after explosion
        Console.WriteLine("Grid after explosion:");
        for (int y = 0; y < GRID_HEIGHT; y++)
        {
            string row = "";
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                row += _grid[x, y] + " ";
            }
            Console.WriteLine(row);
        }
    }
    
    private void ExplodeDirection(int startX, int startY, int dx, int dy, int range)
    {
        for (int i = 1; i <= range; i++)
        {
            int newX = startX + (dx * i);
            int newY = startY + (dy * i);
            
            // Check if out of bounds
            if (newX < 0 || newX >= GRID_WIDTH || newY < 0 || newY >= GRID_HEIGHT)
            {
                Console.WriteLine($"Explosion hit boundary at {newX}, {newY}");
                break;
            }
            
            // Add explosion at this position
            _explosions.Add(new Explosion(newX, newY, 0.5)); // 0.5 second explosion duration
            Console.WriteLine($"Added explosion at {newX}, {newY}, cell type: {_grid[newX, newY]}");
            
            // If we hit a solid block, stop explosion in this direction
            if (_grid[newX, newY] == 1) // Solid block
            {
                Console.WriteLine($"Explosion hit solid block at {newX}, {newY}");
                break;
            }
            
            // If we hit a destructible block, destroy it and stop explosion
            if (_grid[newX, newY] == 2) // Destructible block
            {
                Console.WriteLine($"Destroying block at {newX}, {newY}");
                _grid[newX, newY] = 0; // Destroy the block
                break;
            }
        }
    }
    
    private void RenderBombs()
    {
        // Load bomb texture
        int bombTexture = Renderer.LoadTexture("Assets/BombExploding.png", out _);
        
        foreach (var bomb in _bombs)
        {
            // Render bomb at grid position
            var sourceRect = new Rectangle<int>(0, 0, GRID_SIZE, GRID_SIZE);
            var destRect = new Rectangle<int>(bomb.GridX * GRID_SIZE, bomb.GridY * GRID_SIZE, GRID_SIZE, GRID_SIZE);
            Renderer.RenderTexture(bombTexture, sourceRect, destRect);
        }
    }
    
    private void RenderExplosions()
    {
        // Load explosion texture
        int explosionTexture = Renderer.LoadTexture("Assets/BombExploding.png", out _);
        
        // Set color for explosion (bright orange/red)
        Renderer.SetTextureColorMod(explosionTexture, 255, 100, 0);
        
        foreach (var explosion in _explosions)
        {
            // Render explosion at grid position
            var sourceRect = new Rectangle<int>(0, 0, GRID_SIZE, GRID_SIZE);
            var destRect = new Rectangle<int>(explosion.GridX * GRID_SIZE, explosion.GridY * GRID_SIZE, GRID_SIZE, GRID_SIZE);
            Renderer.RenderTexture(explosionTexture, sourceRect, destRect);
        }
    }
}
