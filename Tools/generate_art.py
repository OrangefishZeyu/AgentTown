"""Original, deterministic pixel art for AgentTown. Requires Pillow only to regenerate."""
from pathlib import Path
import random, json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / 'Assets/AgentTown/Resources/Town'
ROOT.mkdir(parents=True, exist_ok=True)
rng = random.Random(2417)
W,H=1536,1024
ground=Image.new('RGBA',(W,H),'#95ba82'); d=ImageDraw.Draw(ground)
def rect(draw,box,c): draw.rectangle(tuple(map(int,box)), fill=c)
def grass():
    for y in range(0,H,16):
        for x in range(0,W,16):
            if rng.random()<.28:
                c=rng.choice(['#8db47b','#9dc28a','#a4c58e'])
                rect(d,(x+3,y+6,x+5,y+8),c); rect(d,(x+9,y+3,x+10,y+5),c)
grass()
# River and an inhabited, fenced island.
rect(d,(1294,0,1536,1024),'#658f85'); rect(d,(1304,0,1536,1024),'#70b3b5')
rect(d,(1314,0,1536,1024),'#82c3c0')
for y in range(0,H,24):
    rect(d,(1300,y,1305,y+13),'#b3c5a0')
    for x in range(1330,W,40):
        if rng.random()<.5:
            py=y+rng.randrange(10); rect(d,(x,py,x+15,py+2),'#a9d8cd')
# Gravel paths, tiled edges, quiet plaza.
def path(box):
    x,y,xx,yy=box
    rect(d,(x-5,y-5,xx+5,yy+5),'#779b71'); rect(d,(x-2,y-2,xx+2,yy+2),'#c2c7a2')
    rect(d,box,'#e2d5b1')
    for py in range(y+5,yy,16):
        for px in range(x+5,xx,16):
            if rng.random()<.3: rect(d,(px,py,px+2,py+1),'#cabc99')
path((64,495,1293,563)); path((709,70,777,947))
path((274,365,330,755)); path((976,365,1032,769))
path((295,365,1003,414)); path((295,740,1003,790))
rect(d,(614,435,876,666),'#a7b99b');rect(d,(620,441,870,660),'#c0c5aa')
for y in range(442,660,22):
    for x in range(620,870,22):
        rect(d,(x+1,y+1,x+20,y+20),rng.choice(['#dbd8bd','#d2d4ba','#e1dbc0']))
# Wooden bridge.
rect(d,(1270,489,1510,569),'#6d705a')
for x in range(1274,1510,12): rect(d,(x,492,x+9,566),'#b99671');rect(d,(x,495,x+9,497),'#d1b28a')
rect(d,(1268,483,1518,490),'#705a4c');rect(d,(1268,568,1518,576),'#705a4c')
for x in range(1270,1520,48):rect(d,(x,477,x+6,495),'#967651');rect(d,(x,563,x+6,581),'#967651')
# garden plots / paved doorstep platforms
for x,y,w,h in [(139,368,328,34),(864,370,298,32),(148,808,308,30),(866,827,304,32)]:
    rect(d,(x,y,x+w,y+h),'#b4bea1')
    for px in range(x,x+w,24):rect(d,(px+1,y+1,px+22,y+h-2),'#d6cfb0')
# Flower beds
for x,y,w,h in [(120,433,130,30),(390,433,130,30),(906,440,130,27),(1074,440,100,27),(470,720,74,120),(1110,685,76,120)]:
    rect(d,(x-3,y-3,x+w+3,y+h+3),'#8b9c70');rect(d,(x,y,x+w,y+h),'#657e5a')
    for py in range(y+5,y+h,12):
        for px in range(x+5,x+w,12):
            rect(d,(px,py+2,px+2,py+6),'#9bb575')
            rect(d,(px-2,py-1,px+4,py+2),rng.choice(['#edb3a1','#f1d6a5','#c5c1dc']))
# Dock and picnic lawn
rect(d,(1328,720,1478,828),'#688e7a')
for y in range(720,830,10):rect(d,(1328,y,1478,y+7),'#bba17a')
# outer fence
for x in range(36,1260,32):
    for y in [43,962]:
        rect(d,(x,y,x+4,y+22),'#a99b77');rect(d,(x,y,x+4,y+3),'#d7cba1')
for y in [49,57,968,976]:rect(d,(36,y,1260,y+3),'#c6b68d')
ground.save(ROOT/'ground.png')

def building(name,roof,wall,trim,variant):
    im=Image.new('RGBA',(320,256)); p=ImageDraw.Draw(im)
    p.ellipse((12,222,317,255),fill=(43,67,54,58))
    rect(p,(20,104,301,230),'#596956');rect(p,(24,105,297,225),wall)
    for y in range(110,220,16):rect(p,(25,y,297,y+2),'#c7c4a4')
    rect(p,(20,222,301,233),'#9a9d85');rect(p,(28,224,294,227),'#e1ddbe')
    # windows with deep teal glass and warm reflections
    for x in [43,107,210,266]:
        if x==266: continue
        rect(p,(x-4,148,x+42,193),trim);rect(p,(x,152,x+38,188),'#476c6b')
        rect(p,(x+3,154,x+34,162),'#9dc6bd');rect(p,(x+4,165,x+14,184),'#779d92')
        rect(p,(x+18,151,x+21,189),'#e8d7ae');rect(p,(x-6,191,x+44,196),'#e9dcba')
    rect(p,(158,164,193,226),trim);rect(p,(163,169,188,226),'#596c60')
    rect(p,(167,174,184,197),'#abc6ad');rect(p,(183,207,186,210),'#eac77a')
    rect(p,(152,228,199,236),'#d7cfac');rect(p,(148,236,204,241),'#b1b397')
    # gently stepped tiled roof
    p.polygon([(12,117),(43,28),(271,28),(311,117)], fill='#475e57')
    p.polygon([(15,110),(46,27),(270,27),(306,110)], fill=roof)
    colors={'teal':['#598c86','#629890','#75aaa0'], 'rose':['#b77b76','#ca8b82','#d69a8c'], 'gold':['#c6a372','#d4b480','#ddc391'], 'blue':['#738697','#8297a4','#99acb3']}[variant]
    for y in range(34,110,12):
        left=int(46-(y-27)*.37); right=int(270+(y-27)*.44)
        rect(p,(left,y,right,y+2),colors[0])
        for x in range(left+4,right,22):rect(p,(x,y+3,x+2,y+10),colors[1])
    rect(p,(45,24,271,30),colors[2]);rect(p,(13,111,308,119),trim)
    rect(p,(12,119,310,124),'#536455')
    # chimney
    rect(p,(245,17,266,62),'#8a8d7b');rect(p,(242,13,270,20),'#c3bea3')
    for y in [30,43,56]:rect(p,(246,y,265,y+2),'#a7a48d')
    # signboard drawn separately in Unity for clean text
    rect(p,(105,125,231,145),trim);rect(p,(109,128,227,142),'#e9dfc0')
    if variant=='rose':
        for x in range(30,299,18):rect(p,(x,139,x+16,150), '#e5b496' if (x//18)%2 else '#f2dfb5')
    # planters
    for x in [27,274]:
        rect(p,(x,213,x+18,229),'#ad8062');rect(p,(x-2,211,x+20,216),'#c59c79')
        p.ellipse((x-4,193,x+22,215),fill='#6e946d');rect(p,(x+4,195,x+8,198),'#eac9a0')
    im.save(ROOT/(name+'.png'))
building('lab','#50867f','#e1d9b7','#526e64','teal')
building('studio','#75899b','#dfd8c6','#667b83','blue')
building('cafe','#bd8379','#e9d7b1','#936c5b','rose')
building('library','#c3a06d','#dfd6bb','#8a8165','gold')

def tree(name,cherry=False):
    im=Image.new('RGBA',(96,128));p=ImageDraw.Draw(im)
    p.ellipse((10,102,88,126),fill=(41,66,48,52));rect(p,(43,69,54,111),'#786b51');rect(p,(43,74,46,105),'#a2946b')
    cs=['#9d7e87','#bf9198','#d6a6ac','#e6bec1','#f0d1cd'] if cherry else ['#426e56','#54815d','#6b9669','#82a977','#a1bd88']
    for box,c in [((10,43,89,91),cs[0]),((4,37,83,78),cs[1]),((18,11,88,68),cs[1]),((21,3,72,56),cs[2]),((7,28,64,69),cs[2]),((31,14,85,64),cs[3])]:
        p.rounded_rectangle(box,radius=15,fill=c)
    r=random.Random(name)
    for _ in range(52):
        x,y=r.randrange(15,79),r.randrange(16,72)
        rect(p,(x,y,x+r.randrange(2,6),y+2),r.choice(cs[2:]))
    im.save(ROOT/(name+'.png'))
tree('tree');tree('cherry',True)

im=Image.new('RGBA',(128,132));p=ImageDraw.Draw(im)
p.ellipse((4,97,124,129),fill=(50,73,58,55));p.ellipse((9,48,119,117),fill='#8b9e91');p.ellipse((9,42,119,107),fill='#d7d6bb');p.ellipse((18,47,110,97),fill='#6ca6a4');p.ellipse((25,51,104,89),fill='#91c4bb')
p.ellipse((49,48,79,78),fill='#d6d7c1');rect(p,(58,27,70,63),'#a9c4ba');p.ellipse((43,19,85,41),fill='#e1dfc3');p.ellipse((48,17,80,31),fill='#89b9b0');rect(p,(61,6,66,25),'#d7e7d8')
for x,y in [(34,67),(83,62),(78,82)]:rect(p,(x,y,x+11,y+2),'#c0e1d1')
im.save(ROOT/'fountain.png')
im=Image.new('RGBA',(72,44));p=ImageDraw.Draw(im)
for y in [7,16,27]:rect(p,(5,y,66,y+6),'#98765b');rect(p,(5,y,66,y+1),'#c6a27b')
for x in [12,56]:rect(p,(x,4,x+4,40),'#586b58')
im.save(ROOT/'bench.png')
im=Image.new('RGBA',(32,96));p=ImageDraw.Draw(im);p.ellipse((3,80,29,93),fill=(36,64,48,50));rect(p,(14,24,18,84),'#5c7568');rect(p,(10,82,23,87),'#647b6c');rect(p,(7,9,26,32),'#536d61');rect(p,(10,12,23,28),'#f5dfab');rect(p,(5,7,28,11),'#567164');rect(p,(10,4,23,7),'#637b6a');im.save(ROOT/'lamp.png')

# Four directions, four animation frames, with coherent silhouettes.
def character(name,shirt,hair,skin):
    im=Image.new('RGBA',(128,192)); p=ImageDraw.Draw(im)
    for direction in range(4):
        for frame in range(4):
            x,y=frame*32,direction*48
            bob=1 if frame in [1,3] else 0
            def r(box,c):rect(p,(x+box[0],y+box[1]+bob,x+box[2],y+box[3]+bob),c)
            step=2 if frame==1 else -2 if frame==3 else 0
            r((9,34,13,43+step),'#465c64');r((18,34,22,43-step),'#465c64')
            r((8,42+step,14,45+step),'#3b4b51');r((17,42-step,24,45-step),'#3b4b51')
            r((7,23,24,35),shirt);r((10,21,21,36),shirt)
            r((6,25,9,34),skin);r((23,25,26,34),skin)
            r((11,19,20,24),skin);r((8,6,24,20),skin);r((6,9,26,16),skin)
            r((8,5,24,10),hair);r((6,8,10,15),hair);r((23,8,26,14),hair)
            r((11,4,22,6),hair)
            if direction==3:r((8,6,24,18),hair);r((10,19,23,21),hair)
            else:
                if direction!=2:r((12,13,13,15),'#394c50')
                if direction!=1:r((21,13,22,15),'#394c50')
                r((16,19,19,19),'#bc8973')
            if name=='zhang' and direction!=3:
                r((10,12,15,16),'#405968');r((19,12,24,16),'#405968');r((15,13,19,13),'#405968')
                r((11,13,14,14),'#afd0cc');r((20,13,23,14),'#afd0cc')
            if name=='wang':r((7,5,25,8),'#c99972');r((5,9,27,11),'#e4c492')
            if name=='player':r((10,23,13,31),'#e2d8b1');r((20,23,22,31),'#e2d8b1')
    im.save(ROOT/(name+'.png'))
character('zhang','#6f98a8','#45515e','#e1b695')
character('li','#bc8279','#66594e','#e5bb99')
character('wang','#d2b376','#745e53','#e8bea3')
character('player','#7d9b76','#66594b','#e6bb99')
print('Created',len(list(ROOT.glob('*.png'))),'original pixel-art assets at',ROOT)
