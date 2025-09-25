using System.Numerics;

namespace sand
{
    public struct Particle(Vector2 position, Color color)
    {
        public Vector2 Position = position;
        public Vector2 Velocity = Vector2.Zero;
        public Color Color = color;
        public const float Radius = 2.0f;
    }

    public class Simulator
    {
        private readonly List<Particle> _particles = [];
        private readonly Random _random = new();
        private const float Gravity = 500.0f; // 9.81 m/s^2
        private const float Friction = 0.8f;
        private const float BounceDamping = 0.3f;
        private const int MaxParticles = 5000;
        private const int ScreenWidth = 1200;
        private const int ScreenHeight = 800;

        public void Run()
        {
            Raylib.InitWindow(ScreenWidth, ScreenHeight, "Sand Simulator");
            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                Update();
                Draw();
            }

            Raylib.CloseWindow();
        }

        private void Update()
        {
            var deltaTime = Raylib.GetFrameTime();
            if (Raylib.IsMouseButtonDown(MouseButton.Left) && _particles.Count < MaxParticles)
            {
                var mousePos = Raylib.GetMousePosition();

                for (var i = 0; i < 3; i++)
                {
                    if (_particles.Count >= MaxParticles) break;

                    var offset = new Vector2(
                        (float)(_random.NextDouble() - 0.5) * 10,
                        (float)(_random.NextDouble() - 0.5) * 10
                    );

                    var sandColor = GetRandomSandColor();
                    var newParticle = new Particle(mousePos + offset, sandColor);
                    _particles.Add(newParticle);
                }
            }

            for (var i = 0; i < _particles.Count; i++)
                UpdateParticle(i, deltaTime);
        }

        private void UpdateParticle(int index, float deltaTime)
        {
            var particle = _particles[index];

            // Apply gravity
            particle.Velocity.Y += Gravity * deltaTime;

            // Apply some air resistance
            particle.Velocity.X *= 0.999f;

            var newPosition = particle.Position + particle.Velocity * deltaTime;

            // Ground collision
            if (newPosition.Y + Particle.Radius >= ScreenHeight)
            {
                newPosition.Y = ScreenHeight - Particle.Radius;
                particle.Velocity.Y *= -BounceDamping;
                particle.Velocity.X *= Friction;

                if (Math.Abs(particle.Velocity.Y) < 50.0f)
                    particle.Velocity.Y = 0;
            }

            // Wall collisions
            if (newPosition.X - Particle.Radius <= 0)
            {
                newPosition.X = Particle.Radius;
                particle.Velocity.X *= -BounceDamping;
            }
            else if (newPosition.X + Particle.Radius >= ScreenWidth)
            {
                newPosition.X = ScreenWidth - Particle.Radius;
                particle.Velocity.X *= -BounceDamping;
            }

            CheckParticleCollisions(index, ref newPosition, ref particle);

            particle.Position = newPosition;
            _particles[index] = particle;
        }

        private void CheckParticleCollisions(int currentIndex, ref Vector2 newPosition, ref Particle currentParticle)
        {
            for (var i = 0; i < _particles.Count; i++)
            {
                if (i == currentIndex) continue;

                var other = _particles[i];
                var diff = newPosition - other.Position;
                var distance = diff.Length();
                const float minDistance = Particle.Radius + Particle.Radius;

                if (distance is >= minDistance or <= 0) continue;
                
                var normal = Vector2.Normalize(diff);

                // Separate the particles
                var overlap = minDistance - distance;
                var separation = normal * (overlap * 0.5f);

                newPosition += separation;

                // Simple collision response - transfer some velocity
                // TODO: IMPROVE
                var relativeVelocity = Vector2.Dot(currentParticle.Velocity - other.Velocity, normal);

                if (relativeVelocity > 0) continue; // Particles are separating

                const float restitution = 0.2f; // Low bounce for sand
                var impulse = -(1 + restitution) * relativeVelocity / 2; // Assuming equal mass

                currentParticle.Velocity += impulse * normal;

                // Apply friction to simulate sand sticking together
                var tangent = currentParticle.Velocity - Vector2.Dot(currentParticle.Velocity, normal) * normal;
                currentParticle.Velocity -= tangent * 0.1f;
            }
        }

        private Color GetRandomSandColor() => _random.Next(0, 5) switch
        {
            0 => new Color(194, 178, 128, 255), // Light sand
            1 => new Color(218, 165, 32, 255), // Golden sand
            2 => new Color(160, 142, 95, 255), // Dark sand
            3 => new Color(139, 126, 102, 255), // Brown sand
            _ => new Color(205, 192, 176, 255) // Beige sand
        };


        private void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            foreach (var particle in _particles)
                Raylib.DrawCircleV(particle.Position, Particle.Radius, particle.Color);

            Raylib.DrawText("Hold LEFT CLICK to pour sand", 10, 10, 20, Color.White);
            Raylib.DrawText($"Particles: {_particles.Count}/{MaxParticles}", 10, 35, 20, Color.White);
            Raylib.DrawText("Sand Simulator - RaylibCS", 10, ScreenHeight - 30, 20, Color.Gray);

            Raylib.EndDrawing();
        }
    }

    internal static class Program
    {
        private static void Main(string[] _) => new Simulator().Run();
    }
}