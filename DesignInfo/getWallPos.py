from PIL import Image # Pillow used for image modification
from tkinter.filedialog import askopenfilename
import numpy as np

def load(filename):
    sprite_size = (64,64)
    im = Image.open(filename, 'r')
    pix_val = np.array(list(im.getdata()))
    pix_val = pix_val.reshape((sprite_size[0],int(len(pix_val)/sprite_size[0]),4))

    def isBlack(x,y):
        return list(pix_val[x,y]) == [0,0,0,255]
    def isExpand(x,y,rectangles):
        return isBlack(x,y) and ((x,y,1,1) in rectangles)
    rectangles = []
    for y in range(0,sprite_size[1]):
        x = 0
        while x < sprite_size[0]:
            if(isBlack(x,y)):
                w = 1
                while(isBlack(x+w,y)):
                    w=w+1
                rectangles.append((x,y,w,1))
                x=x+w
            else:
                x=x+1
    for x in range(0,sprite_size[0]):
        y = 0
        while (y < sprite_size[1]):
            if(isExpand(x,y,rectangles) and isExpand(x,y+1,rectangles)):
                pre_len = len(rectangles)
                rectangles.remove((x,y,1,1))
                rectangles.remove((x,y+1,1,1))
                h = 2
                while(isExpand(x,y+h,rectangles)):
                    rectangles.remove((x,y+h,1,1))
                    h=h+1
                rectangles.append((x,y,1,h))
                y=y+h
            else:
                y=y+1
    rectangle_json = []
    for x,y,w,h in rectangles:
        rectangle_json.append("{\"x\":%d,\"y\":%d,\"h\":%d,\"w\":%d}" % (y,x-1+w,w,h))
    print("\t\"walls\": [")
    print("\t\t"+",\n\t\t".join(rectangle_json))
    print("]")

prefix = "C:/Users/bryce/Projects/FlockTogether/DesignInfo/"
suffix = ".png"
values = range(0,11)
for value in values:
    print("{")
    filename = prefix+str(value)+suffix
    print("\t\"names\": ["+",".join(["\""+str(x + int(value))+"\"" for x in range(0,66,11)]) + "],")
    load(filename)
    print("},")
