g++ -v -fPIC -shared -lpthread -lwiringPi -lrt -Ofast -o libReadW26.so read.cpp
cp libReadW26.so ../MmsPiFobReader/
