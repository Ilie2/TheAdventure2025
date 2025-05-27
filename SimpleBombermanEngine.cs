using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

// O versiune simplificată a motorului Bomberman
public class SimpleBombermanEngine : Engine
{
    private const int GRID_SIZE = 48; // Dimensiunea fiecărei celule din grilă în pixeli
    private const int GRID_WIDTH = 15;
    private const int GRID_HEIGHT = 13;
    
    // Reprezentare simplă a grilei: 0 = gol, 1 = bloc solid, 2 = bloc destructibil
    private int[,] _grid = new int[GRID_WIDTH, GRID_HEIGHT];
    
    // Referințe către jucători
    private PlayerObject? _player1;
    private PlayerObject? _player2;
    private bool _twoPlayerMode = true; // Setează la true pentru a activa modul cu doi jucători
    
    // Starea jucătorilor
    private bool _player1Alive = true;
    private bool _player2Alive = true;
    
    // Sistemul de scor
    private int _player1Score = 0;
    private int _player2Score = 0;
    
    // Bomb and explosion tracking
    private class Bomb
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public double TimeRemaining { get; set; } // Seconds until explosion
        public int OwnerId { get; set; } // 1 for player 1, 2 for player 2
        
        public Bomb(int x, int y, double time, int ownerId = 1)
        {
            GridX = x;
            GridY = y;
            TimeRemaining = time;
            OwnerId = ownerId;
        }
    }
    
    private class Explosion
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public double TimeRemaining { get; set; } // Seconds until explosion disappears
        public int OwnerId { get; set; } // 1 for player 1, 2 for player 2
        
        public Explosion(int x, int y, double time, int ownerId = 1)
        {
            GridX = x;
            GridY = y;
            TimeRemaining = time;
            OwnerId = ownerId;
        }
    }
    
    private List<Bomb> _bombs = new List<Bomb>();
    private List<Explosion> _explosions = new List<Explosion>();
    
    // Player 1 controls
    private bool _player1BombKeyWasPressed = false;
    
    // Player 2 controls
    private bool _player2BombKeyWasPressed = false;
    
    // Player stats
    private int _player1BombRange = 2;
    private int _player1MaxBombs = 1;
    
    private int _player2BombRange = 2;
    private int _player2MaxBombs = 1;
    
    public SimpleBombermanEngine(GameRenderer renderer, Input input) : base(renderer, input)
    {
        InitializeGrid();
    }
    
    private void InitializeGrid()
    {
        // Inițializează cu spații goale
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                _grid[x, y] = 0; // Gol
            }
        }
        
        // Adaugă blocuri solide într-un model de grilă (la fiecare al doilea bloc)
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                // Ziduri de margine
                if (x == 0 || y == 0 || x == GRID_WIDTH - 1 || y == GRID_HEIGHT - 1)
                {
                    _grid[x, y] = 1; // Solid
                    continue;
                }
                
                // Blocuri solide interioare într-un model de grilă
                if (x % 2 == 0 && y % 2 == 0)
                {
                    _grid[x, y] = 1; // Solid
                }
            }
        }
        
        // Adaugă blocuri destructibile aleatoriu
        Random random = new Random();
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                // Sari peste blocurile solide și pozițiile de start ale jucătorilor
                if (_grid[x, y] == 1)
                    continue;
                    
                // Păstrează pozițiile de start ale jucătorilor libere (colțuri)
                if ((x <= 2 && y <= 2) || // Colțul stânga-sus
                    (x >= GRID_WIDTH - 3 && y <= 2) || // Colțul dreapta-sus
                    (x <= 2 && y >= GRID_HEIGHT - 3) || // Colțul stânga-jos
                    (x >= GRID_WIDTH - 3 && y >= GRID_HEIGHT - 3)) // Colțul dreapta-jos
                {
                    continue;
                }
                
                // 80% șansă de a plasa un bloc destructibil
                if (random.Next(100) < 80)
                {
                    _grid[x, y] = 2; // Destructibil
                }
            }
        }
    }
    
    public override void SetupWorld()
    {
        // Creează jucătorul 1 la poziția de start (colțul stânga-sus)
        _player1 = new PlayerObject(SpriteSheet.Load(Renderer, "Player.json", "Assets"), 
                                  GRID_SIZE + GRID_SIZE/2, GRID_SIZE + GRID_SIZE/2);
        
        // Setează referința principală a jucătorului la jucătorul 1
        _player = _player1;
        
        // Creează jucătorul 2 la poziția de start (colțul dreapta-jos) dacă modul cu doi jucători este activat
        if (_twoPlayerMode)
        {
            _player2 = new PlayerObject(SpriteSheet.Load(Renderer, "Player.json", "Assets"), 
                                      (GRID_WIDTH - 2) * GRID_SIZE + GRID_SIZE/2, 
                                      (GRID_HEIGHT - 2) * GRID_SIZE + GRID_SIZE/2);
        }
        
        // Setează limitele lumii
        Renderer.SetWorldBounds(new Rectangle<int>(0, 0, 
            GRID_WIDTH * GRID_SIZE,
            GRID_HEIGHT * GRID_SIZE));
    }
    
    public override void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var deltaTime = (currentTime - LastUpdate).TotalSeconds;
        LastUpdate = currentTime;
        
        if (_player1 == null)
        {
            return;
        }
        
        // Verificăm dacă jocul s-a terminat (un jucător a câștigat)
        CheckGameOver();
        
        // Procesează inputul jucătorului 1 dacă este în viață
        if (_player1Alive)
        {
            ProcessPlayer1Input(deltaTime);
        }
        
        // Procesează inputul jucătorului 2 dacă modul cu doi jucători este activat și jucătorul este în viață
        if (_twoPlayerMode && _player2 != null && _player2Alive)
        {
            ProcessPlayer2Input(deltaTime);
        }
        
        // Actualizează bombele
        for (int i = _bombs.Count - 1; i >= 0; i--)
        {
            _bombs[i].TimeRemaining -= deltaTime;
            
            // Dacă timpul bombei a expirat, o explodam
            if (_bombs[i].TimeRemaining <= 0)
            {
                Console.WriteLine($"Bomb timer expired at {_bombs[i].GridX}, {_bombs[i].GridY}");
                int range = _bombs[i].OwnerId == 1 ? _player1BombRange : _player2BombRange;
                ExplodeBomb(_bombs[i].GridX, _bombs[i].GridY, range, _bombs[i].OwnerId);
                _bombs.RemoveAt(i);
            }
        }
        
        // Actualizează exploziile și verifică coliziunile cu jucătorii
        for (int i = _explosions.Count - 1; i >= 0; i--)
        {
            _explosions[i].TimeRemaining -= deltaTime;
            
            // Verifică coliziunile cu jucătorii
            CheckExplosionPlayerCollision(_explosions[i]);
            
            // Elimină exploziile expirate
            if (_explosions[i].TimeRemaining <= 0)
            {
                _explosions.RemoveAt(i);
            }
        }
    }
    
    private void CheckGameOver()
    {
        // Verifică dacă unul dintre jucători a murit
        if (!_player1Alive && _player2Alive)
        {
            // Jucătorul 2 a câștigat
            Console.WriteLine("=== GAME OVER ===\nPlayer 2 wins!");
            Console.WriteLine($"Score: Player 1: {_player1Score} - Player 2: {_player2Score}");
        }
        else if (_player1Alive && !_player2Alive)
        {
            // Jucătorul 1 a câștigat
            Console.WriteLine("=== GAME OVER ===\nPlayer 1 wins!");
            Console.WriteLine($"Score: Player 1: {_player1Score} - Player 2: {_player2Score}");
        }
        else if (!_player1Alive && !_player2Alive)
        {
            // Ambii jucători au murit (egalitate)
            Console.WriteLine("=== GAME OVER ===\nDraw! Both players died!");
            Console.WriteLine($"Score: Player 1: {_player1Score} - Player 2: {_player2Score}");
        }
    }
    
    private void CheckExplosionPlayerCollision(Explosion explosion)
    {
        if (_player1 != null && _player1Alive)
        {
            // Obține poziția jucătorului 1 în grid
            int player1GridX = (int)(_player1.Position.X / GRID_SIZE);
            int player1GridY = (int)(_player1.Position.Y / GRID_SIZE);
            
            // Verifică dacă jucătorul 1 este pe aceeași celulă cu explozia
            if (player1GridX == explosion.GridX && player1GridY == explosion.GridY)
            {
                // Jucătorul 1 a fost lovit de explozie
                _player1Alive = false;
                Console.WriteLine("Player 1 was killed by an explosion!");
                
                // Dacă jucătorul 2 a pus bomba, crește scorul jucătorului 2
                if (explosion.OwnerId == 2)
                {
                    _player2Score++;
                    Console.WriteLine($"Player 2 scored a point! Score: {_player2Score}");
                }
            }
        }
        
        if (_player2 != null && _player2Alive)
        {
            // Obține poziția jucătorului 2 în grid
            int player2GridX = (int)(_player2.Position.X / GRID_SIZE);
            int player2GridY = (int)(_player2.Position.Y / GRID_SIZE);
            
            // Verifică dacă jucătorul 2 este pe aceeași celulă cu explozia
            if (player2GridX == explosion.GridX && player2GridY == explosion.GridY)
            {
                // Jucătorul 2 a fost lovit de explozie
                _player2Alive = false;
                Console.WriteLine("Player 2 was killed by an explosion!");
                
                // Dacă jucătorul 1 a pus bomba, crește scorul jucătorului 1
                if (explosion.OwnerId == 1)
                {
                    _player1Score++;
                    Console.WriteLine($"Player 1 scored a point! Score: {_player1Score}");
                }
            }
        }
    }
    
    private void ProcessPlayer1Input(double deltaTime)
    {
        if (_player1 == null) return;
        
        // Procesează inputul jucătorului 1 folosind WASD
        double up = Input.IsKeyWPressed() ? 1.0 : 0.0;
        double down = Input.IsKeySPressed() ? 1.0 : 0.0;
        double left = Input.IsKeyAPressed() ? 1.0 : 0.0;
        double right = Input.IsKeyDPressed() ? 1.0 : 0.0;
        bool isAttacking = Input.IsKeySpacePressed() && (up + down + left + right <= 1);
        
        // Stochează poziția curentă înainte de actualizare
        var oldX = _player1.Position.X;
        var oldY = _player1.Position.Y;
        
        // Actualizează poziția jucătorului
        _player1.UpdatePosition(up, down, left, right, GRID_SIZE, GRID_SIZE, 
                              deltaTime * 1000); // Convertește înapoi la ms pentru actualizarea jucătorului
        
        // Verifică coliziunile cu blocurile
        int gridX = (int)(_player1.Position.X / GRID_SIZE);
        int gridY = (int)(_player1.Position.Y / GRID_SIZE);
        
        // Asigură-te că suntem în limitele grilei
        if (gridX < 0 || gridX >= GRID_WIDTH || gridY < 0 || gridY >= GRID_HEIGHT)
        {
            _player1.Position = (oldX, oldY);
        }
        // Dacă lovim un bloc, revenim la poziția veche
        else if (_grid[gridX, gridY] != 0)
        {
            _player1.Position = (oldX, oldY);
        }
        
        // Gestionează atacul
        if (isAttacking)
        {
            _player1.Attack();
        }
        
        // Gestionează plasarea bombelor cu detecția apăsării și eliberării tastei
        bool bombKeyPressed = Input.IsKeySpacePressed();
        if (bombKeyPressed && !_player1BombKeyWasPressed)
        {
            // Obține poziția grilei pentru bombă
            int bombGridX = gridX;
            int bombGridY = gridY;
            
            // Plasează bomba doar dacă celula este goală, nu există deja o bombă acolo, și jucătorul nu a atins numărul maxim de bombe
            bool cellHasBomb = _bombs.Any(b => b.GridX == bombGridX && b.GridY == bombGridY);
            int player1CurrentBombs = _bombs.Count(b => b.OwnerId == 1);
            
            if (_grid[bombGridX, bombGridY] == 0 && !cellHasBomb && player1CurrentBombs < _player1MaxBombs)
            {
                // Creează o nouă bombă pentru jucătorul 1
                _bombs.Add(new Bomb(bombGridX, bombGridY, 2.0, 1)); // 2 secunde timp de explozie, jucătorul 1
                Console.WriteLine($"Player 1 bomb placed at grid position: {bombGridX}, {bombGridY}");
            }
        }
        _player1BombKeyWasPressed = bombKeyPressed;
    }
    
    private void ProcessPlayer2Input(double deltaTime)
    {
        if (_player2 == null) return;
        
        // Procesează inputul jucătorului 2 folosind săgețile
        double up = Input.IsUpPressed() ? 1.0 : 0.0;
        double down = Input.IsDownPressed() ? 1.0 : 0.0;
        double left = Input.IsLeftPressed() ? 1.0 : 0.0;
        double right = Input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = false; // Nu există atac pentru jucătorul 2 momentan
        
        // Stochează poziția curentă
        var oldX = _player2.Position.X;
        var oldY = _player2.Position.Y;
        
        // Actualizează poziția jucătorului cu multiplicator de viteză
        _player2.UpdatePosition(up, down, left, right, GRID_SIZE, GRID_SIZE, 
                              deltaTime * 1000); // Convertește înapoi la ms pentru actualizarea jucătorului
        
        // Verifică coliziunile cu blocurile
        int gridX = (int)(_player2.Position.X / GRID_SIZE);
        int gridY = (int)(_player2.Position.Y / GRID_SIZE);
        
        // Asigură-te că suntem în limitele grilei
        if (gridX < 0 || gridX >= GRID_WIDTH || gridY < 0 || gridY >= GRID_HEIGHT)
        {
            _player2.Position = (oldX, oldY);
            gridX = (int)(oldX / GRID_SIZE);
            gridY = (int)(oldY / GRID_SIZE);
        }
        // Dacă lovim un bloc, revenim la poziția veche
        else if (_grid[gridX, gridY] != 0)
        {
            _player2.Position = (oldX, oldY);
            gridX = (int)(oldX / GRID_SIZE);
            gridY = (int)(oldY / GRID_SIZE);
        }
        
        // Gestionează atacul
        if (isAttacking)
        {
            _player2.Attack();
        }
        
        // Gestionează plasarea bombelor cu detecția apăsării și eliberării tastei
        bool bombKeyPressed = Input.IsKeyBPressed();
        if (bombKeyPressed && !_player2BombKeyWasPressed)
        {
            // Obține poziția grilei pentru bombă
            int bombGridX = gridX;
            int bombGridY = gridY;
            
            // Plasează bomba doar dacă celula este goală, nu există deja o bombă acolo, și jucătorul nu a atins numărul maxim de bombe
            bool cellHasBomb = _bombs.Any(b => b.GridX == bombGridX && b.GridY == bombGridY);
            int player2CurrentBombs = _bombs.Count(b => b.OwnerId == 2);
            
            if (_grid[bombGridX, bombGridY] == 0 && !cellHasBomb && player2CurrentBombs < _player2MaxBombs)
            {
                // Creează o nouă bombă pentru jucătorul 2
                _bombs.Add(new Bomb(bombGridX, bombGridY, 2.0, 2)); // 2 secunde timp de explozie, jucătorul 2
                Console.WriteLine($"Player 2 bomb placed at grid position: {bombGridX}, {bombGridY}");
            }
        }
        _player2BombKeyWasPressed = bombKeyPressed;
    }
    
    private void ExplodeBomb(int gridX, int gridY, int range, int ownerId)
    {
        Console.WriteLine($"Exploding bomb at grid position: {gridX}, {gridY} with range {range} by player {ownerId}");
        
        // Adaugă explozia la poziția bombei
        _explosions.Add(new Explosion(gridX, gridY, 0.5, ownerId)); // 0.5 secunde durata exploziei
        
        // Explodează în patru direcții
        ExplodeDirection(gridX, gridY, 1, 0, range, ownerId);  // Dreapta
        ExplodeDirection(gridX, gridY, -1, 0, range, ownerId); // Stânga
        ExplodeDirection(gridX, gridY, 0, 1, range, ownerId);  // Jos
        ExplodeDirection(gridX, gridY, 0, -1, range, ownerId); // Sus
    }
    
    private void ExplodeDirection(int startX, int startY, int dx, int dy, int range, int ownerId)
    {
        for (int i = 1; i <= range; i++)
        {
            int newX = startX + (dx * i);
            int newY = startY + (dy * i);
            
            // Verifică dacă este în afara limitelor
            if (newX < 0 || newX >= GRID_WIDTH || newY < 0 || newY >= GRID_HEIGHT)
            {
                break;
            }
            
            // Adaugă explozia la această poziție
            _explosions.Add(new Explosion(newX, newY, 0.5, ownerId)); // 0.5 secunde durata exploziei
            
            // Dacă lovim un bloc solid, oprim explozia în această direcție
            if (_grid[newX, newY] == 1) // Bloc solid
            {
                break;
            }
            
            // Dacă lovim un bloc destructibil, îl distrugem și oprim explozia
            if (_grid[newX, newY] == 2) // Bloc destructibil
            {
                _grid[newX, newY] = 0; // Distruge blocul
                break;
            }
        }
    }
    
    public override void RenderFrame()
    {
        // Curăță ecranul
        Renderer.SetDrawColor(0, 0, 0, 255);
        Renderer.ClearScreen();
        
        if (_player1 != null)
        {
            var playerPosition = _player1.Position;
            Renderer.CameraLookAt(playerPosition.X, playerPosition.Y);
        }
        
        // Renderează grila
        RenderGrid();
        
        // Renderează bombele
        RenderBombs();
        
        // Renderează exploziile
        RenderExplosions();
        
        // Rendereză jucătorul 1 dacă este în viață
        if (_player1 != null && _player1Alive)
        {
            _player1.Render(Renderer);
        }
        
        // Rendereză jucătorul 2 dacă modul cu doi jucători este activat și jucătorul este în viață
        if (_twoPlayerMode && _player2 != null && _player2Alive)
        {
            _player2.Render(Renderer);
        }
        
        // Renderează obiectele jocului
        RenderAllObjects();
        
        // Prezintă cadrul
        Renderer.PresentFrame();
    }
    
    private void RenderGrid()
    {
        // Folosește doar texturi existente valide, dar cu culori diferite pentru a le face distincte
        int floorTexture = Renderer.LoadTexture("Assets/grass_003.png", out _);
        int solidBlockTexture = Renderer.LoadTexture("Assets/grass_001.png", out _);
        int brickBlockTexture = Renderer.LoadTexture("Assets/grass_002.png", out _);
        
        // Setează culori diferite pentru diferite tipuri de blocuri
        Renderer.SetTextureColorMod(solidBlockTexture, 50, 50, 150); // Albastru închis pentru blocuri solide
        Renderer.SetTextureColorMod(brickBlockTexture, 150, 50, 50); // Roșu închis pentru blocuri destructibile
        
        // Renderează grila
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                int textureId;
                
                // Selectează textura în funcție de tipul blocului
                switch (_grid[x, y])
                {
                    case 1: // Solid
                        textureId = solidBlockTexture;
                        break;
                    case 2: // Destructibil
                        textureId = brickBlockTexture;
                        break;
                    default: // Gol
                        textureId = floorTexture;
                        break;
                }
                
                // Renderează blocul
                var sourceRect = new Rectangle<int>(0, 0, GRID_SIZE, GRID_SIZE);
                var destRect = new Rectangle<int>(x * GRID_SIZE, y * GRID_SIZE, GRID_SIZE, GRID_SIZE);
                Renderer.RenderTexture(textureId, sourceRect, destRect);
            }
        }
    }
    
    private void RenderBombs()
    {
        // Încarcă textura bombei
        int bombTexture = Renderer.LoadTexture("Assets/BombExploding.png", out _);
        
        foreach (var bomb in _bombs)
        {
            // Renderează bomba la poziția grilei
            var sourceRect = new Rectangle<int>(0, 0, GRID_SIZE, GRID_SIZE);
            var destRect = new Rectangle<int>(bomb.GridX * GRID_SIZE, bomb.GridY * GRID_SIZE, GRID_SIZE, GRID_SIZE);
            Renderer.RenderTexture(bombTexture, sourceRect, destRect);
        }
    }
    
    private void RenderExplosions()
    {
        // Încarcă textura exploziei
        int explosionTexture = Renderer.LoadTexture("Assets/BombExploding.png", out _);
        
        foreach (var explosion in _explosions)
        {
            // Setează culoarea pentru explozie în funcție de proprietar (jucătorul 1: portocaliu, jucătorul 2: albastru)
            if (explosion.OwnerId == 1)
                Renderer.SetTextureColorMod(explosionTexture, 255, 100, 0); // Portocaliu pentru jucătorul 1
            else
                Renderer.SetTextureColorMod(explosionTexture, 0, 100, 255); // Albastru pentru jucătorul 2
            
            // Renderează explozia la poziția grilei
            var sourceRect = new Rectangle<int>(0, 0, GRID_SIZE, GRID_SIZE);
            var destRect = new Rectangle<int>(explosion.GridX * GRID_SIZE, explosion.GridY * GRID_SIZE, GRID_SIZE, GRID_SIZE);
            Renderer.RenderTexture(explosionTexture, sourceRect, destRect);
        }
    }
}
