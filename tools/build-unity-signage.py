from pathlib import Path
import json, re, random, math
from PIL import Image,ImageDraw,ImageFont,ImageFilter
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'unity/ZeroRise/Assets/ZeroRise/Art/Signs';OUT.mkdir(parents=True,exist_ok=True)
FONT='C:/Windows/Fonts/arialbd.ttf';NORMAL='C:/Windows/Fonts/arial.ttf'
R=random.Random(190010)
def textfit(d,box,text,fill,font=FONT):
 x,y,w,h=box;size=int(h*.85)
 while size>8:
  f=ImageFont.truetype(font,size);b=d.textbbox((0,0),text,font=f)
  if b[2]-b[0]<=w*.95 and b[3]-b[1]<=h:break
  size-=1
 d.text((x+(w-(b[2]-b[0]))/2-b[0],y+(h-(b[3]-b[1]))/2-b[1]),text,font=f,fill=fill)
def plate(key,lines,w=1024,h=256,bg=(31,59,75),fg=(218,219,207),border=True):
 im=Image.new('RGB',(w,h),bg);d=ImageDraw.Draw(im)
 if border:d.rounded_rectangle((14,14,w-15,h-15),radius=14,outline=fg,width=max(3,h//42))
 margin=h*.16
 for i,line in enumerate(lines):textfit(d,(margin,margin+i*(h-2*margin)/len(lines),w-2*margin,(h-2*margin)/len(lines)),line,fg)
 # Sparse edge chips, fine enamel grain and dirt stay on the physical plate.
 for i in range(w*h//600):
  x=R.randrange(w);y=R.randrange(h)
  if min(x,y,w-x,h-y)<h*.10:d.rectangle((x,y,x+R.randrange(1,5),y+R.randrange(1,3)),fill=(98,91,76))
 pix=im.load()
 for i in range(w*h//11):
  x=R.randrange(w);y=R.randrange(h);c=pix[x,y];n=R.randint(-8,5);pix[x,y]=tuple(max(0,min(255,v+n)) for v in c)
 im.save(OUT/(key+'.png'))
plan=json.loads((ROOT/'unity/ZeroRise/Assets/ZeroRise/Data/Westbruch.json').read_text(encoding='utf-8-sig'))
for b in plan['buildings']:
 n=re.search(r'(\d+)$',b['id']);n=n.group(1) if n else '5' if b['kind']=='shop' else '9' if b['kind']=='workshop' else '7'
 plate('number_'+n,[n],256,256)
names=['Westbruchstra\u00dfe','Nordring','S\u00fcdring','Uferstra\u00dfe','Hofstra\u00dfe','Gleisdamm']
for i,name in enumerate(names):plate('street_'+str(i),[name])
plate('shop',['PFAND & SPAR'],2048,256,(35,66,48),(222,216,185))
plate('shop_goods',['GETR\u00c4NKE  \u00b7  PFAND  \u00b7  LEBENSMITTEL'],2048,180,(35,66,48),(214,211,185),False)
plate('service',['SERVICE & REPARATUR'],2048,256,(55,65,62),(216,211,186))
plate('deliveries',['ANLIEFERUNG','Zufahrt freihalten'],1024,360,(184,180,155),(40,44,40))
plate('garage',['Privatgaragen','Nur f\u00fcr Mieter'],1024,360,(183,180,160),(43,49,44))
plate('storage',['Teilelager'],1024,256,(84,96,82),(222,219,198))
plate('building_site',['BAUARBEITEN','Zutritt verboten'],1024,512,(188,164,67),(37,40,35))
plate('bus_name',['Westbruch'],1024,220,(207,207,187),(37,57,46))
plate('bus_line',['Linie 4  \u00b7  Stadtmitte'],1024,190,(205,205,187),(37,57,46))
im=Image.new('RGB',(512,512),(207,182,56));d=ImageDraw.Draw(im);d.ellipse((25,25,487,487),outline=(30,77,44),width=29);textfit(d,(85,85,342,342),'H',(28,76,41));im.save(OUT/'bus_h.png')
im=Image.new('RGB',(512,512),(210,207,190));d=ImageDraw.Draw(im);d.ellipse((12,12,500,500),fill=(210,207,190),outline=(151,38,28),width=42);textfit(d,(95,118,322,270),'30',(33,35,31));im.save(OUT/'speed_30.png')
plate('timetable',['Linie 4','WESTBRUCH','Stadtmitte','Mo - Fr','06  26  46','07  27  47','08  28  48'],512,768,(205,202,181),(38,49,44))
im=Image.new('RGB',(1024,1024),(174,172,154));d=ImageDraw.Draw(im)
for i in range(11000):
 x=R.randrange(1024);y=R.randrange(1024);r=R.choices([1,2,3,5,8],[6,5,3,1,.15])[0];tone=R.choice([(77,79,73),(118,120,109),(147,146,131),(190,187,166)])
 d.ellipse((x-r,y-r,x+r,y+r),fill=tone)
im.save(OUT/'road_paint.png')
print('Created',len(list(OUT.glob('*.png'))),'deterministic printed sign/paint textures')
