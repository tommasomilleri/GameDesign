from PIL import Image
import os
os.makedirs("docs", exist_ok=True)
c = Image.open("Assets/Drawings/New Assets/Clock.png").convert("RGBA")
h = Image.open("Assets/Drawings/New Assets/ClockHand2.png").convert("RGBA")
new_w = int(c.width * 1.2)
h = h.resize((new_w, int(h.height * new_w / h.width)), Image.LANCZOS)
h = h.rotate(-67, expand=True, resample=Image.BICUBIC)
c.alpha_composite(h, ((c.width - h.width) // 2, (c.height - h.height) // 2))
c.resize((520, int(520 * c.height / c.width)), Image.LANCZOS).save("docs/clock.png")
print("ok -> docs/clock.png")