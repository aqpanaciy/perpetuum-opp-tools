magick ".\altitude.0200.bin.png" -alpha off -precision 10 -format "%[fx:round(maxima*65535)]" info:
magick ".\altitude.0200.bin.png" -alpha off -evaluate Divide "%[fx:maxima>0?maxima:1]" -depth 8 -quality 100 ".\altitude.0200.bin.XXXXX.jpeg"
