from pathlib import Path
from PIL import Image
import json
p=Path(__file__).resolve().parent
frames=json.loads((p/'six-frames.json').read_text(encoding='utf-8'))['FramesList']
out=[]
green=(28,184,128); bg=(22,28,34); orange=(255,153,22)
for frame in frames:
    img=Image.open(p/('six-'+format(frame['Requested'],'.2f')+'.png')).convert('RGB')
    record=dict(requested=frame['Requested'],elapsed=frame['Elapsed'],formalPage=frame['FormalPage'],rows=[])
    for i in range(6):
        y=105+85*i
        main=[img.getpixel((x,y)) for x in range(100,580)]
        assert all(c==green for c in main), (i,frame['Requested'],'main not fully real/green')
        colors=[]; start=580; last=img.getpixel((580,y))
        for x in range(581,901):
            c=img.getpixel((x,y)) if x<900 else None
            if c!=last:
                colors.append(dict(fromX=start,toX=x-1,rgb=last));start=x;last=c
        row=dict(mainPixelRange=[100,579],mainGreenPixels=480,lossSegments=colors,text=frame['Rows'][i]['Text'])
        assert row['text']=='60%'
        if frame['Requested']>1:
            assert all(img.getpixel((x,y))==bg for x in range(580,900)), (i,'terminal remnant')
        if .30<frame['Requested']<.35:
            assert all(img.getpixel((x,y))==(orange if i>=3 else bg) for x in range(580,900)), (i,'white did not retire')
        record['rows'].append(row)
    out.append(record)
(p/'independent-pixel-assertions.json').write_text(json.dumps(dict(passed=True,frames=out),indent=2),encoding='utf-8')
print('Passed five frames × six rows: 480 real main pixels each; white retirement and terminal remnants checked.')
