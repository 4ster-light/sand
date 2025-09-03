using System.Numerics;

namespace sand;

static class Program
{
    static void Main(string[] _) => new Simulator().Run();
}

public struct Particle(Vector2 position, Color color)
{
    public Vector2 Position = position;
    public Vector2 Velocity = Vector2.Zero;
    public Color Color = color;
    public float Radius = 2.0f;
    public bool IsSettled = false;
}

public class Simulator
{
    private readonly List<Particle> particles;
    private readonly Random random;
    private const float GRAVITY = 500.0f; // 9.81 m/s^2
    private const float FRICTION = 0.8f;
    private const float BOUNCE_DAMPING = 0.3f;
    private const int MAX_PARTICLES = 5000;
    private readonly int screenWidth = 1200;
    private readonly int screenHeight = 800;

    public Simulator()
    {
        particles = [];
        random = new Random();
    }

    public void Run()
    {
        Raylib.InitWindow(screenWidth, screenHeight, "Sand Simulator");
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
        float deltaTime = Raylib.GetFrameTime();

        // Add new particles when left mouse button is held
        if (Raylib.IsMouseButtonDown(MouseButton.Left) && particles.Count < MAX_PARTICLES)
        {
            var mousePos = Raylib.GetMousePosition();

            // Add multiple particles per frame for continuous stream
            for (int i = 0; i < 3; i++)
            {
                if (particles.Count >= MAX_PARTICLES) break;

                var offset = new Vector2(
                    (float)(random.NextDouble() - 0.5) * 10,
                    (float)(random.NextDouble() - 0.5) * 10
                );

                var sandColor = GetRandomSandColor();
                var newParticle = new Particle(mousePos + offset, sandColor);
                particles.Add(newParticle);
            }
        }

        // Update physics for all particles
        for (int i = 0; i < particles.Count; i++)
        {
            UpdateParticle(i, deltaTime);
        }

        // Remove particles that fall off screen
        particles.RemoveAll(p => p.Position.Y > screenHeight + 50);
    }

    private void UpdateParticle(int index, float deltaTime)
    {
        var particle = particles[index];

        // Apply gravity
        particle.Velocity.Y += GRAVITY * deltaTime;

        // Apply some air resistance
        particle.Velocity.X *= 0.999f;

        // Update position
        var newPosition = particle.Position + particle.Velocity * deltaTime;

        // Ground collision
        if (newPosition.Y + particle.Radius >= screenHeight)
        {
            newPosition.Y = screenHeight - particle.Radius;
            particle.Velocity.Y *= -BOUNCE_DAMPING;
            particle.Velocity.X *= FRICTION;

            if (Math.Abs(particle.Velocity.Y) < 50.0f)
            {
                particle.Velocity.Y = 0;
                particle.IsSettled = true;
            }
        }

        // Wall collisions
        if (newPosition.X - particle.Radius <= 0)
        {
            newPosition.X = particle.Radius;
            particle.Velocity.X *= -BOUNCE_DAMPING;
        }
        else if (newPosition.X + particle.Radius >= screenWidth)
        {
            newPosition.X = screenWidth - particle.Radius;
            particle.Velocity.X *= -BOUNCE_DAMPING;
        }

        // Particle-to-particle collisions
        CheckParticleCollisions(index, ref newPosition, ref particle);

        particle.Position = newPosition;
        particles[index] = particle;
    }

    private void CheckParticleCollisions(int currentIndex, ref Vector2 newPosition, ref Particle currentParticle)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            if (i == currentIndex) continue;

            var other = particles[i];
            var diff = newPosition - other.Position;
            var distance = diff.Length();
            var minDistance = currentParticle.Radius + other.Radius;

            if (distance < minDistance && distance > 0)
            {
                // Normalize the collision vector
                var normal = Vector2.Normalize(diff);

                // Separate the particles
                var overlap = minDistance - distance;
                var separation = normal * (overlap * 0.5f);

                newPosition += separation;

                // Simple collision response - transfer some velocity
                var relativeVelocity = Vector2.Dot(currentParticle.Velocity - other.Velocity, normal);

                if (relativeVelocity > 0) continue; // Particles are separating

                var restitution = 0.2f; // Low bounce for sand
                var impulse = -(1 + restitution) * relativeVelocity / 2; // Assuming equal mass

                var impulseVector = impulse * normal;
                currentParticle.Velocity += impulseVector;

                // Apply friction to simulate sand sticking together
                var tangent = currentParticle.Velocity - Vector2.Dot(currentParticle.Velocity, normal) * normal;
                currentParticle.Velocity -= tangent * 0.1f;

                // Mark as unsettled if significant collision
                if (Math.Abs(impulse) > 10.0f)
                {
                    currentParticle.IsSettled = false;
                }
            }
        }
    }

    // Generate various shades of sand colors
    private Color GetRandomSandColor() => random.Next(0, 5) switch
    {
        0 => new Color(194, 178, 128, 255), // Light sand
        1 => new Color(218, 165, 32, 255),  // Golden sand
        2 => new Color(160, 142, 95, 255),  // Dark sand
        3 => new Color(139, 126, 102, 255), // Brown sand
        _ => new Color(205, 192, 176, 255)  // Beige sand
    };


    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        // Draw all particles
        foreach (var particle in particles)
        {
            Raylib.DrawCircleV(particle.Position, particle.Radius, particle.Color);
        }

        // Draw UI
        Raylib.DrawText("Hold LEFT CLICK to pour sand", 10, 10, 20, Color.White);
        Raylib.DrawText($"Particles: {particles.Count}/{MAX_PARTICLES}", 10, 35, 20, Color.White);
        Raylib.DrawText("Sand Simulator - RaylibCS", 10, screenHeight - 30, 20, Color.Gray);

        Raylib.EndDrawing();
    }
}
