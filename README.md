## Introduction

Done as a part of artificial intelligence course. The tasks were:

* generate sift image descriptor for 2 images
* from the cloud of points create pairs so that both points in the pair belong to different images
* filter the pairs so that only the pairs with greatest cohesion are left
* generate affine transformation that translates first image in the second one based on the points cloud ( using RANdom SAmple Consensus algorithm to find the transformation)


As doing the computations is expensive there is quite a lot of cheating involved. 

* lookup caches
* high parallelization
* swapping parts of algorithm that use sqrt to a simpler methods ( that '*may not exactly work in the same way as the original*')
* skippping validation checks - when it is faster to calculate something 5 times without validation then it is to do it once properly why even bother validating ?


I've had a hard time finding some good (free) C# profiler, so there are some profiling logging statements left in the code. It is nice to know that the changes You are doing do make a difference !


## General observations

* [Parallel.for] is AWESOME !
* WPF is very friendly for novices to get the buttons where they want them to go, but not so nice when You have some styling problem
* C# is a little bit irritating when You try to use it without reading a book. I haven't felt that about scripting languages, when often just pasting a snippet from SO is enough.


## Screenshots

![img1]
*Both images of the book captured by me*

## Usage

#### Prepare the image

1. Download http://www.robots.ox.ac.uk/~vgg/research/affine/det_eval_files/extract_features2.tar.gz
1. Generate image descriptor using downloaded script:
`./extract_features_32bit.ln -haraff -sift -i IMAGE.png -DE`


#### Add the image to program

1. Copy the .png and .png.haraff.sift file to **data** folder
1. Open [Main]
1. Add new position to the IMAGES_ENUM following the template:
  ```
  [Description("IMAGE_FILE_NAME.png")]
  UNIQUE_VALUE
  ```
1. Run the program


## Other

Enclosed report is in polish.


[Parallel.for]: http://msdn.microsoft.com/en-us/library/system.threading.tasks.parallel.for(v=vs.110).aspx
[img1]: https://raw.github.com/Scthe/Image_ArtificialIntelligence/master/programImage.png
[Main]: MainWindow.xaml.cs
