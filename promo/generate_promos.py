"""
Generates the promo images for Little Evolution Simulation.

The scenes are produced by a small Python port of the rules in
Assets/Scripts/Creature.cs and MapMaster.cs, and drawn using the colors,
font, and UI layout found in Assets/Scenes/Simulation.unity.

Usage: python3 promo/generate_promos.py
"""

import math
import os
import random

import cairo

OUT_DIR = os.path.dirname(os.path.abspath(__file__))
W, H = 1920, 1080

# Palette (from the Unity scenes / prefabs)
BG = (0x19 / 255, 0x1A / 255, 0x1F / 255)        # Main Camera background
WALL = (0x39 / 255, 0x35 / 255, 0x41 / 255)      # Wall.prefab
ORANGE = (0xEE / 255, 0x96 / 255, 0x4B / 255)    # Food.prefab + UI titles
TEXT = (0.988, 0.988, 0.988)                     # TMP text color
PANEL = (0, 0, 0, 0.196)                         # UI panel background
BUTTON = (0, 0, 0)                               # Buttons

# Gene accents used to explain the color gene
GENE_R = (0.93, 0.33, 0.33)
GENE_G = (0.36, 0.85, 0.45)
GENE_B = (0.36, 0.55, 0.98)

FONT = "Liberation Sans"
MONO = "Liberation Mono"

MAX_SPEED = 16.0
MAX_VIEW = 15.0
MAX_WANDER = 360.0


# ---------------------------------------------------------------------------
# Genome helpers (mirror Creature.InheritGenome)
# ---------------------------------------------------------------------------

def random_genome(rng):
    bits = "".join(str(rng.randint(0, 1)) for _ in range(24))
    return " ".join(bits[i:i + 4] for i in range(0, 24, 4))


def mutate(genome, rng):
    parts = genome.split(" ")
    i = rng.randrange(len(parts))
    j = rng.randrange(4)
    bits = list(parts[i])
    bits[j] = "0" if bits[j] == "1" else "1"
    parts[i] = "".join(bits)
    return " ".join(parts), i * 4 + j


def traits(genome):
    p = genome.split(" ")
    speed = int(p[0] + p[1], 2) / 255 * MAX_SPEED
    view = int(p[2] + p[3], 2) / 255 * MAX_VIEW
    wander = int(p[4] + p[5], 2) / 255 * MAX_WANDER
    return speed, view, wander


def genome_color(genome):
    v = int(genome.replace(" ", ""), 2)
    return ((v >> 16) & 0xFF) / 255, ((v >> 8) & 0xFF) / 255, (v & 0xFF) / 255


def genome_hex(genome):
    r, g, b = genome_color(genome)
    return "#%02X%02X%02X" % (round(r * 255), round(g * 255), round(b * 255))


# ---------------------------------------------------------------------------
# Tiny simulation (simplified port of Creature.cs / MapMaster.cs)
# ---------------------------------------------------------------------------

class SimCell:
    def __init__(self, x, y, genome, generation, rng, parent=None):
        self.x, self.y = x, y
        self.genome = genome
        self.generation = generation
        self.speed, self.view, self.wander = traits(genome)
        # Keep everyone moving a little so the scene reads as alive
        self.speed = max(self.speed, 2.0)
        self.color = genome_color(genome)
        self.energy = 0.25
        a = rng.uniform(0, math.tau)
        self.vx, self.vy = math.cos(a) * self.speed, math.sin(a) * self.speed
        self.trail = []
        self.parent = parent
        self.id = id(self)


class Sim:
    def __init__(self, seed, world=100, cells=20, food=500, spawn_every=0.1,
                 mutation=0.5, genomes=None):
        self.rng = random.Random(seed)
        self.world = world
        self.half = world / 2
        self.spawn_every = spawn_every
        self.mutation = mutation
        self.t = 0.0
        self.spawn_t = 0.0
        self.cells = []
        self.food = []
        self.history = []
        for i in range(cells):
            g = genomes[i % len(genomes)] if genomes else random_genome(self.rng)
            self.cells.append(SimCell(*self.rand_pos(), g, 1, self.rng))
        for _ in range(food):
            self.food.append(self.rand_pos())

    def rand_pos(self):
        h = self.half - 1
        return self.rng.uniform(-h, h), self.rng.uniform(-h, h)

    def step(self, dt=0.05, trail_len=14):
        rng = self.rng
        self.t += dt
        self.spawn_t += dt
        while self.spawn_t >= self.spawn_every:
            self.spawn_t -= self.spawn_every
            if len(self.food) < 900:
                self.food.append(self.rand_pos())

        newborn = []
        for c in list(self.cells):
            # Energy cost (1/2 mv^2 flavoured, scaled for this port)
            c.energy -= (c.speed ** 2 * c.view / 100000 * 6 + 0.012) * dt
            if c.energy <= 0:
                self.cells.remove(c)
                continue

            # Divide
            if c.energy >= 1:
                c.energy /= 2
                g = c.genome
                if rng.random() < self.mutation:
                    g, _ = mutate(g, rng)
                child = SimCell(c.x, c.y, g, c.generation + 1, rng, parent=c)
                child.energy = c.energy
                newborn.append(child)

            # Find nearest food within view radius (view distance / 2)
            r = c.view / 2 + 0.5
            best, bd = None, r * r
            for f in self.food:
                dx, dy = f[0] - c.x, f[1] - c.y
                d = dx * dx + dy * dy
                if d < bd:
                    best, bd = f, d

            if best is not None:
                dx, dy = best[0] - c.x, best[1] - c.y
                d = math.sqrt(bd) or 1
                tx, ty = dx / d, dy / d
            else:
                # Wander: rotate heading by +/- wanderDeviation
                sp = math.hypot(c.vx, c.vy) or 1
                hx, hy = c.vx / sp, c.vy / sp
                ang = math.radians(c.wander) * rng.choice((-1, 1)) * dt * 3
                tx = math.cos(ang) * hx - math.sin(ang) * hy
                ty = math.sin(ang) * hx + math.cos(ang) * hy

            c.vx = c.vx * 0.8 + tx * c.speed * 0.35
            c.vy = c.vy * 0.8 + ty * c.speed * 0.35
            sp = math.hypot(c.vx, c.vy)
            if sp > c.speed:
                c.vx, c.vy = c.vx / sp * c.speed, c.vy / sp * c.speed

            c.x += c.vx * dt
            c.y += c.vy * dt
            # Bounce off walls
            lim = self.half - 1
            if abs(c.x) > lim:
                c.x = math.copysign(lim, c.x)
                c.vx = -c.vx
            if abs(c.y) > lim:
                c.y = math.copysign(lim, c.y)
                c.vy = -c.vy

            # Eat
            if best is not None and bd <= 1.0:
                self.food.remove(best)
                c.energy += 0.5

            c.trail.append((c.x, c.y))
            if len(c.trail) > trail_len:
                c.trail.pop(0)

        self.cells.extend(newborn)
        self.history.append((self.t, len(self.cells), len(self.food)))

    def run(self, seconds, dt=0.05, max_cells=None):
        for _ in range(int(seconds / dt)):
            self.step(dt)
            if not self.cells or (max_cells and len(self.cells) >= max_cells):
                break


# ---------------------------------------------------------------------------
# Drawing helpers
# ---------------------------------------------------------------------------

def new_canvas():
    surf = cairo.ImageSurface(cairo.FORMAT_ARGB32, W, H)
    ctx = cairo.Context(surf)
    ctx.set_source_rgb(*BG)
    ctx.paint()
    return surf, ctx


def font(ctx, size, bold=False, mono=False):
    ctx.select_font_face(MONO if mono else FONT, cairo.FONT_SLANT_NORMAL,
                         cairo.FONT_WEIGHT_BOLD if bold else cairo.FONT_WEIGHT_NORMAL)
    ctx.set_font_size(size)


def text(ctx, s, x, y, size, color=TEXT, bold=False, mono=False, align="left", alpha=1.0):
    font(ctx, size, bold, mono)
    ext = ctx.text_extents(s)
    if align == "center":
        x -= ext.x_advance / 2
    elif align == "right":
        x -= ext.x_advance
    ctx.set_source_rgba(*color[:3], alpha)
    ctx.move_to(x, y)
    ctx.show_text(s)
    return ext.x_advance


def text_width(ctx, s, size, bold=False, mono=False):
    font(ctx, size, bold, mono)
    return ctx.text_extents(s).x_advance


def rect(ctx, x, y, w, h, rgba):
    ctx.set_source_rgba(*rgba) if len(rgba) == 4 else ctx.set_source_rgb(*rgba)
    ctx.rectangle(x, y, w, h)
    ctx.fill()


def rounded(ctx, x, y, w, h, r):
    ctx.new_path()
    ctx.arc(x + w - r, y + r, r, -math.pi / 2, 0)
    ctx.arc(x + w - r, y + h - r, r, 0, math.pi / 2)
    ctx.arc(x + r, y + h - r, r, math.pi / 2, math.pi)
    ctx.arc(x + r, y + r, r, math.pi, 3 * math.pi / 2)
    ctx.close_path()


def circle(ctx, x, y, r, rgba):
    ctx.new_path()
    ctx.arc(x, y, r, 0, math.tau)
    ctx.set_source_rgba(*rgba) if len(rgba) == 4 else ctx.set_source_rgb(*rgba)
    ctx.fill()


def ring(ctx, x, y, r, width, rgba, dash=None):
    ctx.save()
    if dash:
        ctx.set_dash(dash)
    ctx.new_path()
    ctx.arc(x, y, r, 0, math.tau)
    ctx.set_line_width(width)
    ctx.set_source_rgba(*rgba)
    ctx.stroke()
    ctx.restore()


def line(ctx, x1, y1, x2, y2, width, rgba, dash=None):
    ctx.save()
    if dash:
        ctx.set_dash(dash)
    ctx.new_path()
    ctx.move_to(x1, y1)
    ctx.line_to(x2, y2)
    ctx.set_line_width(width)
    ctx.set_source_rgba(*rgba)
    ctx.stroke()
    ctx.restore()


def arrow(ctx, x1, y1, x2, y2, width=3, rgba=(1, 1, 1, 0.5), head=14):
    line(ctx, x1, y1, x2, y2, width, rgba)
    a = math.atan2(y2 - y1, x2 - x1)
    ctx.move_to(x2, y2)
    ctx.line_to(x2 - head * math.cos(a - 0.45), y2 - head * math.sin(a - 0.45))
    ctx.line_to(x2 - head * math.cos(a + 0.45), y2 - head * math.sin(a + 0.45))
    ctx.close_path()
    ctx.set_source_rgba(*rgba)
    ctx.fill()


def soft_glow(ctx, x, y, r, rgb, alpha=0.35):
    g = cairo.RadialGradient(x, y, 0, x, y, r)
    g.add_color_stop_rgba(0, *rgb, alpha)
    g.add_color_stop_rgba(1, *rgb, 0)
    ctx.set_source(g)
    ctx.new_path()
    ctx.arc(x, y, r, 0, math.tau)
    ctx.fill()


def vignette(ctx):
    g = cairo.RadialGradient(W / 2, H / 2, H * 0.35, W / 2, H / 2, W * 0.75)
    g.add_color_stop_rgba(0, 0, 0, 0, 0)
    g.add_color_stop_rgba(1, 0, 0, 0, 0.45)
    ctx.set_source(g)
    ctx.paint()


def draw_cell(ctx, x, y, r, color, trail=None, view_r=None, view_alpha=0.12, view_w=1.2):
    """A cell exactly as the prefab looks: view ring, tapered trail, circle sprite."""
    if view_r:
        ring(ctx, x, y, view_r, view_w, (1, 1, 1, view_alpha))
    if trail and len(trail) > 1:
        n = len(trail)
        for i in range(1, n):
            t = i / (n - 1)
            (ax, ay), (bx, by) = trail[i - 1], trail[i]
            ctx.set_line_cap(cairo.LINE_CAP_ROUND)
            line(ctx, ax, ay, bx, by, max(0.5, 2 * r * t), (*color, 0.85 * t * t))
    circle(ctx, x, y, r, color)


def panel(ctx, x, y, w, h):
    rect(ctx, x, y, w, h, PANEL)


def slider(ctx, x, y, w, value, label):
    text(ctx, label, x, y, 30)
    sy = y + 22
    rounded(ctx, x, sy, w, 14, 7)
    ctx.set_source_rgba(1, 1, 1, 0.18)
    ctx.fill()
    rounded(ctx, x, sy, max(14, w * value), 14, 7)
    ctx.set_source_rgb(*TEXT)
    ctx.fill()
    circle(ctx, x + w * value, sy + 7, 13, TEXT)
    circle(ctx, x + w * value, sy + 7, 5, BG)


def button(ctx, x, y, w, h, label):
    rect(ctx, x, y, w, h, BUTTON)
    text(ctx, label, x + w / 2, y + h / 2 + 11, 30, align="center")


def title_block(ctx, x, y, size=84, align="left", sub=None, sub_size=30):
    """Main menu title: 'Little Evolution Simulation' in the UI orange."""
    font(ctx, size, bold=True)
    small = size * 0.42
    if align == "center":
        tw = text_width(ctx, "EVOLUTION SIMULATION", size, bold=True)
        x0 = x - tw / 2
    else:
        x0 = x
    text(ctx, "LITTLE", x0 + 4, y - size * 0.92, small, color=TEXT, bold=True, alpha=0.7)
    text(ctx, "EVOLUTION SIMULATION", x0, y, size, color=ORANGE, bold=True)
    if sub:
        text(ctx, sub, x0 + 2, y + sub_size + 26, sub_size, alpha=0.75)


def save(surf, name):
    path = os.path.join(OUT_DIR, name)
    surf.write_to_png(path)
    print("wrote", path)


# ---------------------------------------------------------------------------
# 1. Global view: the world + World Information panel
# ---------------------------------------------------------------------------

def promo_world():
    sim = Sim(seed=7, cells=20, food=500)
    sim.run(120, max_cells=90)

    surf, ctx = new_canvas()

    # World sits on the right, like the camera offset (worldSize * 0.3) in the game
    size = 960
    ox, oy = W - size - 70, (H - size) / 2
    s = size / sim.world

    def P(x, y):
        return ox + (x + sim.half) * s, oy + (sim.half - y) * s

    # Walls
    t = s * 1.0
    rect(ctx, ox - t / 2, oy - t / 2, size + t, t, WALL)
    rect(ctx, ox - t / 2, oy + size - t / 2, size + t, t, WALL)
    rect(ctx, ox - t / 2, oy - t / 2, t, size + t, WALL)
    rect(ctx, ox + size - t / 2, oy - t / 2, t, size + t, WALL)

    for f in sim.food:
        circle(ctx, *P(*f), 0.125 * s * 1.3, ORANGE)
    for c in sim.cells:
        trail = [P(*p) for p in c.trail]
        draw_cell(ctx, *P(c.x, c.y), 0.5 * s * 1.15, c.color, trail, c.view / 2 * s)

    vignette(ctx)

    # Left column: title + World Information panel
    lx = 80
    title_block(ctx, lx, 170, size=62,
                sub="Watch tiny cells evolve to survive their world.")

    py, pw = 300, 650
    panel(ctx, lx - 30, py, pw, 700)
    text(ctx, "World Information", lx, py + 60, 40, color=ORANGE)
    text(ctx, "World Size: 100", lx, py + 110, 30)
    text(ctx, "Cell Count: %d" % len(sim.cells), lx, py + 150, 30)
    text(ctx, "Food Count: %d" % len(sim.food), lx, py + 190, 30)

    text(ctx, "World State", lx, py + 270, 40, color=ORANGE)
    sw = pw - 110
    slider(ctx, lx, py + 320, sw, 0.1, "Time Scale: 100%")
    slider(ctx, lx, py + 395, sw, 0.1, "Energy Cost: 100%")
    slider(ctx, lx, py + 470, sw, 0.1, "Food Spawn Rate: 0.10s")
    slider(ctx, lx, py + 545, sw, 0.5, "Mutation Frequency: 50%")

    bw = (pw - 90) / 2
    button(ctx, lx, py + 610, bw, 62, "Stop Simulation")
    button(ctx, lx + bw + 30, py + 610, bw, 62, "Reset Simulation")

    # Live population sparkline (new component)
    hist = sim.history[::4]
    gx, gy, gw, gh = ox + 30, oy + size - 150, 330, 110
    rect(ctx, gx - 14, gy - 40, gw + 28, gh + 56, (0, 0, 0, 0.55))
    text(ctx, "Population", gx, gy - 12, 22, color=ORANGE)
    peak = max(h[1] for h in hist) or 1
    ctx.move_to(gx, gy + gh)
    for i, (_, n, _) in enumerate(hist):
        ctx.line_to(gx + gw * i / (len(hist) - 1), gy + gh - gh * n / peak * 0.9)
    ctx.line_to(gx + gw, gy + gh)
    ctx.close_path()
    ctx.set_source_rgba(*TEXT, 0.12)
    ctx.fill_preserve()
    ctx.new_path()
    for i, (_, n, _) in enumerate(hist):
        X = gx + gw * i / (len(hist) - 1)
        Y = gy + gh - gh * n / peak * 0.9
        ctx.line_to(X, Y)
    ctx.set_source_rgb(*TEXT)
    ctx.set_line_width(2.5)
    ctx.stroke()
    text(ctx, "%d cells" % len(sim.cells), gx + gw, gy - 12, 22, align="right", alpha=0.7)

    save(surf, "promo_1_world.png")


# ---------------------------------------------------------------------------
# 2. Genome decoder
# ---------------------------------------------------------------------------

def promo_genome():
    genome = "1011 0110 1001 1100 0011 1010"
    speed, view, wander = traits(genome)
    color = genome_color(genome)
    parts = genome.split(" ")

    surf, ctx = new_canvas()
    title_block(ctx, W / 2, 175, size=72, align="center")
    text(ctx, "Every cell is built from a 24-bit genome. Its color is its DNA.",
         W / 2, 250, 32, align="center", alpha=0.75)

    # Genome strip
    gene_cols = [GENE_R, GENE_R, GENE_G, GENE_G, GENE_B, GENE_B]
    bit = 52
    gap_n = 26
    total = 24 * bit + 5 * gap_n
    x0 = (W - total) / 2
    y0 = 330
    panel(ctx, x0 - 40, y0 - 30, total + 80, bit + 120)
    text(ctx, "Cell Genome", x0, y0 + 2, 30, color=ORANGE)
    y0 += 24
    for n, p in enumerate(parts):
        nx = x0 + n * (4 * bit + gap_n)
        col = gene_cols[n]
        for i, b in enumerate(p):
            bx = nx + i * bit
            rect(ctx, bx + 3, y0 + 3, bit - 6, bit - 6,
                 (*col, 0.95 if b == "1" else 0.18))
            text(ctx, b, bx + bit / 2, y0 + bit / 2 + 12, 32, bold=True, mono=True,
                 align="center", color=(BG if b == "1" else TEXT))
    # Brackets under the gene pairs
    labels = [("MOVE SPEED", GENE_R), ("VIEW DISTANCE", GENE_G), ("WANDER DEVIATION", GENE_B)]
    by = y0 + bit + 22
    centers = []
    for k, (lab, col) in enumerate(labels):
        a = x0 + (2 * k) * (4 * bit + gap_n)
        b = a + 8 * bit + gap_n
        line(ctx, a + 4, by, b - 4, by, 3, (*col, 1))
        line(ctx, a + 4, by - 10, a + 4, by, 3, (*col, 1))
        line(ctx, b - 4, by - 10, b - 4, by, 3, (*col, 1))
        centers.append((a + b) / 2)
        text(ctx, lab, (a + b) / 2, by + 34, 22, bold=True, color=col, align="center")

    # Trait cards
    cy, ch = 560, 330
    cw = 420
    cx0 = 110
    gapc = 40
    cards = [
        ("Move Speed", GENE_R, int(parts[0] + parts[1], 2), MAX_SPEED, "%.0f" % speed, "faster, but hungrier"),
        ("View Distance", GENE_G, int(parts[2] + parts[3], 2), MAX_VIEW, "%.0f" % view, "spots food from afar"),
        ("Wander Deviation", GENE_B, int(parts[4] + parts[5], 2), MAX_WANDER, "%.0f°" % wander, "how far it turns while searching"),
    ]
    for k, (name, col, raw, mx, val, note) in enumerate(cards):
        x = cx0 + k * (cw + gapc)
        panel(ctx, x, cy, cw, ch)
        rect(ctx, x, cy, cw, 6, (*col, 1))
        text(ctx, name, x + 30, cy + 58, 36, color=ORANGE)
        text(ctx, "%s → %d / 255 × %g" % (parts[2 * k] + parts[2 * k + 1], raw, mx),
             x + 30, cy + 100, 22, mono=True, alpha=0.7)
        text(ctx, val, x + 30, cy + 190, 72, bold=True)
        text(ctx, note, x + 30, cy + 230, 24, alpha=0.6)

        # Little visual per trait
        vx, vy = x + cw - 100, cy + 160
        if k == 0:
            for j in range(5):
                line(ctx, vx - 95 + j * 6, vy - 30 + j * 15, vx - 30, vy - 30 + j * 15, 4,
                     (*col, 0.25 + 0.12 * j))
            draw_cell(ctx, vx + 10, vy, 26, color)
        elif k == 1:
            r = view / MAX_VIEW * 85
            ring(ctx, vx, vy, r, 2, (1, 1, 1, 0.35))
            circle(ctx, vx, vy, 16, color)
            circle(ctx, vx + r * 0.6, vy - r * 0.5, 6, ORANGE)
            circle(ctx, vx - r * 0.75, vy + r * 0.2, 6, ORANGE)
        else:
            a = math.radians(wander)
            ctx.move_to(vx, vy)
            ctx.arc(vx, vy, 70, -math.pi / 2 - a / 2, -math.pi / 2 + a / 2)
            ctx.close_path()
            ctx.set_source_rgba(*col, 0.25)
            ctx.fill()
            for sgn in (-1, 1):
                ang = -math.pi / 2 + sgn * a / 2
                arrow(ctx, vx, vy, vx + 78 * math.cos(ang), vy + 78 * math.sin(ang), 3,
                      (*col, 0.9), 12)
            circle(ctx, vx, vy, 16, color)

        # bar showing the raw byte
        bx, bw_ = x + 30, cw - 60
        rounded(ctx, bx, cy + ch - 50, bw_, 12, 6)
        ctx.set_source_rgba(1, 1, 1, 0.12)
        ctx.fill()
        rounded(ctx, bx, cy + ch - 50, bw_ * raw / 255, 12, 6)
        ctx.set_source_rgba(*col, 1)
        ctx.fill()

    # Resulting color swatch on the far right
    sx = cx0 + 3 * (cw + gapc)
    sw = W - sx - 110
    panel(ctx, sx, cy, sw, ch)
    text(ctx, "Color", sx + sw / 2, cy + 58, 36, color=ORANGE, align="center")
    soft_glow(ctx, sx + sw / 2, cy + 160, 110, color, 0.5)
    circle(ctx, sx + sw / 2, cy + 160, 52, color)
    text(ctx, genome_hex(genome), sx + sw / 2, cy + 262, 32, bold=True, mono=True, align="center")
    hx = sx + sw / 2 - 60
    for j, col in enumerate((GENE_R, GENE_G, GENE_B)):
        text(ctx, "RGB"[j], hx + j * 60, cy + 305, 22, bold=True, color=col, align="center")

    text(ctx, "Red = speed   ·   Green = sight   ·   Blue = wander.  "
              "Similar colors mean similar cells.",
         W / 2, 990, 28, align="center", alpha=0.7)

    save(surf, "promo_2_genome.png")


# ---------------------------------------------------------------------------
# 2b. Genome decoder, explanation only (no title)
# ---------------------------------------------------------------------------

def step_heading(ctx, n, label, x, y, note=None):
    circle(ctx, x + 22, y - 12, 22, ORANGE)
    text(ctx, str(n), x + 22, y - 1, 28, bold=True, color=BG, align="center")
    text(ctx, label, x + 62, y, 34, bold=True)
    if note:
        text(ctx, note, W - 110, y, 24, align="right", alpha=0.55)


def promo_genome_v2():
    genome = "1011 0110 1001 1100 0011 1010"
    speed, view, wander = traits(genome)
    color = genome_color(genome)
    parts = genome.split(" ")
    gene_cols = [GENE_R, GENE_R, GENE_G, GENE_G, GENE_B, GENE_B]

    surf, ctx = new_canvas()

    # Step 1: the genome strip
    step_heading(ctx, 1, "Every cell carries a 24-bit genome", 110, 100,
                 note="three 8-bit genes")
    bit, gap_n = 52, 26
    total = 24 * bit + 5 * gap_n
    x0 = (W - total) / 2
    y0 = 165
    panel(ctx, 110, y0 - 32, W - 220, 175)
    for n, p in enumerate(parts):
        nx = x0 + n * (4 * bit + gap_n)
        for i, b in enumerate(p):
            bx = nx + i * bit
            rect(ctx, bx + 3, y0 + 3, bit - 6, bit - 6,
                 (*gene_cols[n], 0.95 if b == "1" else 0.18))
            text(ctx, b, bx + bit / 2, y0 + bit / 2 + 12, 32, bold=True, mono=True,
                 align="center", color=(BG if b == "1" else TEXT))
    labels = [("MOVE SPEED", GENE_R), ("VIEW DISTANCE", GENE_G), ("WANDER DEVIATION", GENE_B)]
    by = y0 + bit + 22
    for k, (lab, col) in enumerate(labels):
        a = x0 + (2 * k) * (4 * bit + gap_n)
        b = a + 8 * bit + gap_n
        line(ctx, a + 4, by, b - 4, by, 3, (*col, 1))
        line(ctx, a + 4, by - 10, a + 4, by, 3, (*col, 1))
        line(ctx, b - 4, by - 10, b - 4, by, 3, (*col, 1))
        text(ctx, lab, (a + b) / 2, by + 34, 22, bold=True, color=col, align="center")

    # Step 2: each gene is scaled into a trait, and all 24 bits form the color
    step_heading(ctx, 2, "Each gene is scaled into a trait", 110, 395,
                 note="value / 255 × trait maximum")
    cy, ch = 425, 340
    cw, gapc, cx0 = 420, 40, 110
    cards = [
        ("Move Speed", GENE_R, MAX_SPEED, "%.0f" % speed, "faster, but burns more energy"),
        ("View Distance", GENE_G, MAX_VIEW, "%.0f" % view, "spots food from farther away"),
        ("Wander Deviation", GENE_B, MAX_WANDER, "%.0f°" % wander, "how sharply it turns to search"),
    ]
    for k, (name, col, mx, val, note) in enumerate(cards):
        x = cx0 + k * (cw + gapc)
        raw = int(parts[2 * k] + parts[2 * k + 1], 2)
        panel(ctx, x, cy, cw, ch)
        rect(ctx, x, cy, cw, 6, (*col, 1))
        text(ctx, name, x + 30, cy + 58, 34, color=ORANGE)
        text(ctx, "%s = %d" % (parts[2 * k] + parts[2 * k + 1], raw),
             x + 30, cy + 98, 22, mono=True, alpha=0.7)
        text(ctx, val, x + 30, cy + 205, 76, bold=True)
        text(ctx, "of %g max" % mx, x + 32, cy + 240, 22, alpha=0.5)
        text(ctx, note, x + 30, cy + 280, 22, alpha=0.7)

        vx, vy = x + cw - 105, cy + 185
        if k == 0:
            for j in range(5):
                line(ctx, vx - 95 + j * 6, vy - 30 + j * 15, vx - 30, vy - 30 + j * 15, 4,
                     (*col, 0.25 + 0.12 * j))
            draw_cell(ctx, vx + 10, vy, 26, color)
        elif k == 1:
            r = view / MAX_VIEW * 80
            ring(ctx, vx, vy, r, 2, (1, 1, 1, 0.35))
            circle(ctx, vx, vy, 16, color)
            circle(ctx, vx + r * 0.6, vy - r * 0.5, 6, ORANGE)
            circle(ctx, vx - r * 0.75, vy + r * 0.2, 6, ORANGE)
        else:
            vy += 25
            a = math.radians(wander)
            ctx.move_to(vx, vy)
            ctx.arc(vx, vy, 62, -math.pi / 2 - a / 2, -math.pi / 2 + a / 2)
            ctx.close_path()
            ctx.set_source_rgba(*col, 0.25)
            ctx.fill()
            for sgn in (-1, 1):
                ang = -math.pi / 2 + sgn * a / 2
                arrow(ctx, vx, vy, vx + 70 * math.cos(ang), vy + 70 * math.sin(ang), 3,
                      (*col, 0.9), 12)
            circle(ctx, vx, vy, 16, color)

        bx, bw_ = x + 30, cw - 60
        rounded(ctx, bx, cy + ch - 34, bw_, 12, 6)
        ctx.set_source_rgba(1, 1, 1, 0.12)
        ctx.fill()
        rounded(ctx, bx, cy + ch - 34, bw_ * raw / 255, 12, 6)
        ctx.set_source_rgba(*col, 1)
        ctx.fill()

    sx = cx0 + 3 * (cw + gapc)
    sw = W - sx - 110
    panel(ctx, sx, cy, sw, ch)
    text(ctx, "Color", sx + sw / 2, cy + 58, 34, color=ORANGE, align="center")
    soft_glow(ctx, sx + sw / 2, cy + 150, 100, color, 0.5)
    circle(ctx, sx + sw / 2, cy + 150, 48, color)
    text(ctx, genome_hex(genome), sx + sw / 2, cy + 245, 32, bold=True, mono=True, align="center")
    text(ctx, "all 24 bits read as RGB", sx + sw / 2, cy + 285, 20, align="center", alpha=0.6)
    for j, col in enumerate((GENE_R, GENE_G, GENE_B)):
        X = sx + sw / 2 + (j - 1) * 60
        text(ctx, "RGB"[j], X, cy + ch - 22, 22, bold=True, color=col, align="center")

    # Step 3: inheritance + mutation
    child_parts = list(parts)
    child_parts[0] = ("0" if parts[0][0] == "1" else "1") + parts[0][1:]
    child = " ".join(child_parts)
    pc, cc = color, genome_color(child)

    step_heading(ctx, 3, "Offspring inherit it, sometimes with one flipped bit", 110, 835)
    text(ctx, "A mutation changes both a trait and the color, so similar colors mean similar cells.",
         172, 873, 24, alpha=0.6)

    py = 905
    panel(ctx, 110, py, W - 220, 120)
    cw_ = text_width(ctx, "0", 26, mono=True)
    gx = 230
    for row, (g, col, lab) in enumerate(((genome, pc, "Parent"), (child, cc, "Child"))):
        X0 = gx + row * 670
        circle(ctx, X0 - 70, py + 52, 22, col)
        text(ctx, lab, X0 - 70, py + 102, 18, align="center", alpha=0.6)
        for i, ch_ in enumerate(g):
            if ch_ == " ":
                continue
            X = X0 + i * cw_
            hit = row == 1 and i == 0
            if hit:
                rect(ctx, X - 2, py + 60 - 23, cw_ + 4, 32, (*ORANGE, 1))
            text(ctx, ch_, X, py + 60 + 2, 26, mono=True, bold=hit,
                 color=BG if hit else TEXT)
    gw = text_width(ctx, genome, 26, mono=True)
    arrow(ctx, gx + gw + 30, py + 52, gx + 670 - 110, py + 52, 3, (*ORANGE, 0.9), 12)
    text(ctx, "Move Speed  %.0f → %.0f" % (speed, traits(child)[0]),
         W - 140, py + 62, 26, color=ORANGE, align="right")

    save(surf, "promo_2_genome_v2.png")


# ---------------------------------------------------------------------------
# 3. Eat -> Divide -> Mutate life cycle
# ---------------------------------------------------------------------------

def promo_lifecycle():
    rng = random.Random(3)
    parent = "1001 0011 1100 1010 0110 0101"
    child, flip = "1001 0011 1100 1010 0110 0101", None
    child_parts = child.split(" ")
    # Flip a bit in the blue (wander) gene so the color change is visible
    b = list(child_parts[4])
    b[0] = "1" if b[0] == "0" else "0"
    child_parts[4] = "".join(b)
    child = " ".join(child_parts)
    flip = 16
    pc, cc = genome_color(parent), genome_color(child)

    surf, ctx = new_canvas()
    title_block(ctx, 110, 175, size=72)
    text(ctx, "Eat.", W - 110 - text_width(ctx, "Eat. Divide. Mutate.", 64, bold=True),
         160, 64, bold=True)
    tw = text_width(ctx, "Eat. ", 64, bold=True)
    x = W - 110 - text_width(ctx, "Eat. Divide. Mutate.", 64, bold=True)
    text(ctx, "Divide.", x + tw, 160, 64, bold=True)
    tw2 = text_width(ctx, "Eat. Divide. ", 64, bold=True)
    text(ctx, "Mutate.", x + tw2, 160, 64, bold=True, color=ORANGE)

    # Four stage panels
    sy, sh = 270, 520
    sw = 395
    gap = 30
    sx0 = (W - (4 * sw + 3 * gap)) / 2
    stages = [
        ("1  Search", "Wanders until food enters\nits view distance.", 0.30),
        ("2  Eat", "Each bite of food adds\n+50% energy.", 0.80),
        ("3  Divide", "At 100% energy it splits.\nBoth halves keep 50%.", 1.00),
        ("4  Mutate", "Sometimes a single bit flips.\nNew genes, new color.", 0.50),
    ]
    for k, (name, desc, energy) in enumerate(stages):
        x = sx0 + k * (sw + gap)
        panel(ctx, x, sy, sw, sh)
        text(ctx, name, x + 28, sy + 56, 36, color=ORANGE)
        for j, ln in enumerate(desc.split("\n")):
            text(ctx, ln, x + 28, sy + 100 + j * 32, 24, alpha=0.75)

        cx, cy = x + sw / 2, sy + 275
        if k == 0:
            trail = []
            for j in range(26):
                t = j / 25
                trail.append((cx - 130 + 120 * t, cy + 60 - 50 * math.sin(t * 3.2)))
            draw_cell(ctx, cx - 10, trail[-1][1], 20, pc, trail, view_r=85, view_alpha=0.3, view_w=2)
            fx, fy = cx + 70, cy - 40
            circle(ctx, fx, fy, 7, ORANGE)
            for f in [(cx + 140, cy + 80), (cx - 150, cy - 90), (cx + 120, cy - 120)]:
                circle(ctx, *f, 7, ORANGE)
            arrow(ctx, cx + 12, cy + 2, fx - 14, fy + 8, 2.5, (1, 1, 1, 0.6), 10)
        elif k == 1:
            ring(ctx, cx, cy, 85, 2, (1, 1, 1, 0.3))
            soft_glow(ctx, cx, cy, 70, ORANGE, 0.35)
            trail = [(cx - 110 + 4 * j, cy + 30 - 1.2 * j) for j in range(26)]
            draw_cell(ctx, cx, cy, 24, pc, trail)
            for j in range(6):
                a = j / 6 * math.tau + 0.3
                circle(ctx, cx + 40 * math.cos(a), cy + 40 * math.sin(a), 3, (*ORANGE, 0.8))
            text(ctx, "+50%", cx + 40, cy - 48, 34, bold=True, color=ORANGE)
        elif k == 2:
            ring(ctx, cx, cy, 110, 1.5, (1, 1, 1, 0.12), dash=[6, 6])
            draw_cell(ctx, cx - 44, cy, 22, pc)
            draw_cell(ctx, cx + 44, cy, 22, pc)
            ctx.save()
            ctx.set_source_rgba(*pc, 0.35)
            ctx.arc(cx, cy, 14, 0, math.tau)
            ctx.fill()
            ctx.restore()
            arrow(ctx, cx - 8, cy - 40, cx - 40, cy - 70, 2.5, (1, 1, 1, 0.5), 10)
            arrow(ctx, cx + 8, cy - 40, cx + 40, cy - 70, 2.5, (1, 1, 1, 0.5), 10)
            text(ctx, "Gen 7", cx - 44, cy + 62, 22, align="center", alpha=0.7)
            text(ctx, "Gen 8", cx + 44, cy + 62, 22, align="center", alpha=0.7)
        else:
            soft_glow(ctx, cx + 60, cy, 90, cc, 0.35)
            draw_cell(ctx, cx - 70, cy, 26, pc)
            arrow(ctx, cx - 30, cy, cx + 20, cy, 3, (1, 1, 1, 0.6), 12)
            draw_cell(ctx, cx + 62, cy, 26, cc)
            text(ctx, genome_hex(parent), cx - 70, cy + 64, 20, mono=True, align="center", alpha=0.7)
            text(ctx, genome_hex(child), cx + 62, cy + 64, 20, mono=True, align="center",
                 color=ORANGE)

        # Energy bar (mirrors "Energy: N%" in Cell Information)
        ex, ey, ew = x + 28, sy + sh - 62, sw - 56
        text(ctx, "Energy: %d%%" % round(energy * 100), ex, ey - 12, 24)
        rounded(ctx, ex, ey, ew, 16, 8)
        ctx.set_source_rgba(1, 1, 1, 0.12)
        ctx.fill()
        rounded(ctx, ex, ey, max(16, ew * energy), 16, 8)
        ctx.set_source_rgb(*(ORANGE if k == 1 else TEXT))
        ctx.fill()

        if k < 3:
            arrow(ctx, x + sw + 4, sy + sh / 2, x + sw + gap - 4, sy + sh / 2, 3,
                  (*ORANGE, 0.9), 10)

    # Genome diff strip (new component)
    gy = 860
    font(ctx, 34, mono=True)
    gw = text_width(ctx, parent, 34, mono=True)
    lx = 260
    panel(ctx, 110, gy - 50, W - 220, 170)
    text(ctx, "Parent", 140, gy, 26, alpha=0.7)
    text(ctx, "Child", 140, gy + 70, 26, alpha=0.7)
    cw_ = text_width(ctx, "0", 34, mono=True)
    for row, g in enumerate((parent, child)):
        idx = 0
        for i, ch_ in enumerate(g):
            X = lx + i * cw_
            if ch_ == " ":
                continue
            hit = row == 1 and idx == flip
            if hit:
                rect(ctx, X - 2, gy + 70 - 34, cw_ + 4, 44, (*ORANGE, 1))
            text(ctx, ch_, X, gy + row * 70, 34, mono=True,
                 color=BG if hit else TEXT, bold=hit)
            idx += 1
    ax = lx + gw + 50
    text(ctx, "One flipped bit in the wander gene", ax, gy + 8, 28)
    text(ctx, "Wander Deviation  %.0f°  →  %.0f°"
         % (traits(parent)[2], traits(child)[2]), ax, gy + 52, 28, color=ORANGE)
    text(ctx, "Energy cost grows with speed² × view distance, so every trait is a trade-off.",
         ax, gy + 92, 22, alpha=0.6)

    save(surf, "promo_3_lifecycle.png")


# ---------------------------------------------------------------------------
# 4. Cell view: following one cell + its lineage
# ---------------------------------------------------------------------------

def promo_cellview():
    sim = Sim(seed=11, cells=20, food=700, mutation=0.6)
    sim.run(120, max_cells=260)
    # Follow the deepest-generation cell
    central = [c for c in sim.cells if abs(c.x) < 28 and abs(c.y) < 28] or sim.cells
    target = max(central, key=lambda c: (c.generation, c.energy))
    lineage = []
    c = target
    while c is not None:
        lineage.append(c)
        c = c.parent
    lineage.reverse()

    surf, ctx = new_canvas()

    # Camera follows target (zoomed-in cell view)
    s = 34.0
    cxw, cyw = target.x, target.y
    vx0 = 760  # screen x of target

    def P(x, y):
        return vx0 + (x - cxw) * s, H / 2 + 10 - (y - cyw) * s

    half = sim.half
    for (x1, y1, x2, y2) in ((-half, -half, half, -half), (-half, half, half, half),
                             (-half, -half, -half, half), (half, -half, half, half)):
        a, b = P(x1, y1), P(x2, y2)
        line(ctx, *a, *b, s, (*WALL, 1))

    for f in sim.food:
        X, Y = P(*f)
        if -20 < X < W + 20 and -20 < Y < H + 20:
            circle(ctx, X, Y, 0.125 * s, ORANGE)
    for c in sim.cells:
        X, Y = P(c.x, c.y)
        if -300 < X < W + 300 and -300 < Y < H + 300:
            trail = [P(*p) for p in c.trail]
            draw_cell(ctx, X, Y, 0.5 * s, c.color, trail, c.view / 2 * s, view_w=1.5)

    # Highlight the followed cell (new component: selection reticle)
    X, Y = P(target.x, target.y)
    soft_glow(ctx, X, Y, 70, target.color, 0.4)
    draw_cell(ctx, X, Y, 0.5 * s, target.color)
    ring(ctx, X, Y, 0.5 * s + 12, 2.5, (*ORANGE, 1))
    for a in range(4):
        ang = a * math.pi / 2 + math.pi / 4
        r1, r2 = 0.5 * s + 20, 0.5 * s + 34
        line(ctx, X + r1 * math.cos(ang), Y + r1 * math.sin(ang),
             X + r2 * math.cos(ang), Y + r2 * math.sin(ang), 3, (*ORANGE, 1))
    vr = target.view / 2 * s
    ring(ctx, X, Y, vr, 2, (1, 1, 1, 0.35), dash=[8, 8])
    text(ctx, "view distance", X, Y - vr - 14, 22, align="center", alpha=0.75)

    vignette(ctx)

    # Scrim so the title reads over the busy world
    g = cairo.RadialGradient(380, 150, 60, 380, 150, 620)
    g.add_color_stop_rgba(0, *BG, 0.92)
    g.add_color_stop_rgba(0.55, *BG, 0.75)
    g.add_color_stop_rgba(1, *BG, 0)
    ctx.set_source(g)
    ctx.paint()

    # Title (top-left)
    title_block(ctx, 80, 160, size=64, sub="Click any cell to follow it through the world.",
                sub_size=28)

    # Cell Information panel (right, mirrors UI.SetCellInformation)
    pw = 600
    px, py = W - pw - 60, 70
    panel(ctx, px, py, pw, 690)
    rect(ctx, px, py, pw, 690, (*BG, 0.88))
    lx = px + 40
    text(ctx, "Cell Genome", lx, py + 64, 40, color=ORANGE)
    text(ctx, "Generation: %d" % target.generation, lx, py + 114, 30)
    text(ctx, target.genome, lx, py + 158, 30, mono=True)

    text(ctx, "Characteristics", lx, py + 240, 40, color=ORANGE)
    sp, vw, wd = traits(target.genome)
    text(ctx, "Color: %s" % genome_hex(target.genome), lx, py + 292, 30)
    circle(ctx, lx + text_width(ctx, "Color: %s" % genome_hex(target.genome), 30) + 28,
           py + 282, 12, target.color)
    rows = [("Move Speed: %.0f" % sp, sp / MAX_SPEED, GENE_R),
            ("View Distance: %.0f" % vw, vw / MAX_VIEW, GENE_G),
            ("Wander Deviation: %.0f°" % wd, wd / MAX_WANDER, GENE_B)]
    for j, (lab, frac, col) in enumerate(rows):
        yy = py + 344 + j * 66
        text(ctx, lab, lx, yy, 30)
        rounded(ctx, lx, yy + 14, pw - 80, 8, 4)
        ctx.set_source_rgba(1, 1, 1, 0.1)
        ctx.fill()
        rounded(ctx, lx, yy + 14, max(8, (pw - 80) * frac), 8, 4)
        ctx.set_source_rgba(*col, 1)
        ctx.fill()

    e = min(target.energy, 0.99)
    text(ctx, "Energy: %d%%" % round(e * 100), lx, py + 560, 30)
    rounded(ctx, lx, py + 578, pw - 80, 16, 8)
    ctx.set_source_rgba(1, 1, 1, 0.12)
    ctx.fill()
    rounded(ctx, lx, py + 578, max(16, (pw - 80) * e), 16, 8)
    ctx.set_source_rgb(*ORANGE)
    ctx.fill()
    text(ctx, "Esc  global view     Space  stop following", lx, py + 650, 22, alpha=0.5)

    # Lineage strip (new component): ancestors back to generation 1
    ly = 860
    lw = W - 120
    panel(ctx, 60, ly - 50, lw, 170)
    rect(ctx, 60, ly - 50, lw, 170, (*BG, 0.88))
    text(ctx, "Lineage", 100, ly, 34, color=ORANGE)
    text(ctx, "Generation 1 → %d" % target.generation, 100, ly + 40, 24, alpha=0.6)
    shown = lineage[-14:]
    n = len(shown)
    sx0, sx1 = 400, W - 140
    step = (sx1 - sx0) / max(1, n - 1)
    yy = ly + 25
    line(ctx, sx0, yy, sx0 + step * (n - 1), yy, 2, (1, 1, 1, 0.2))
    for j, c in enumerate(shown):
        X = sx0 + j * step
        last = j == n - 1
        prev = shown[j - 1] if j else None
        changed = prev is not None and prev.genome != c.genome
        r = 22 if last else 16
        if last:
            ring(ctx, X, yy, r + 8, 2.5, (*ORANGE, 1))
        circle(ctx, X, yy, r, c.color)
        if changed:
            circle(ctx, X + r * 0.75, yy - r * 0.75, 6, ORANGE)
        text(ctx, str(c.generation), X, yy + 54, 20, align="center", alpha=0.85 if last else 0.5)
    text(ctx, "● mutation", sx1 + 20, ly - 22, 18, color=ORANGE, align="right")

    save(surf, "promo_4_cellview.png")


if __name__ == "__main__":
    promo_world()
    promo_genome()
    promo_genome_v2()
    promo_lifecycle()
    promo_cellview()
