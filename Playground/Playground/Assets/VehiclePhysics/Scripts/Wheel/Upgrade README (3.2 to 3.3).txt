When upgrading from Wheel Controller 3D version 3.2 to version 3.3 following changes will need to be made to the existing vehicles:

- ScanIgnoreLayer is now ScanIgnoreLayers and is a dropdown with multiple layer select options. 
Instead of typing in the layer number now it needs to be ticked (ticked layers 
will be ignored). Values from the previous version will not be saved so this field will need to be set up.

- Friction calculation changes. Both forward and side friction force coefficients are now about 30% stiffer 
so the value needs to be reduced by 30% for the existing vehicles
to get the same result. e.g. 1.4 (before) is equivalent to (1).