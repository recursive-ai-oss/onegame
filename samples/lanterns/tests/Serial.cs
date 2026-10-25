// Several of these tests start five-process clusters or render 3D frames on the CPU. Run side by side on a two-core machine they starve each
// other, the servers fall behind real time and the swap scenarios miss their deadlines, so the tests run one after the other.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
