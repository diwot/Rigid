#pragma once

#include <Eigen/Geometry>
#include <Eigen/StdVector>

// Functions and classes that allow access to the "As Rigid As Possible" implementation from
// other programming languages than C++
// Libigl is used in order to compile this code https://github.com/libigl/libigl
namespace ExternalAccess
{
	void ArrayToVectorI(int* array, Eigen::VectorXi& vector)
	{
		int rows = vector.rows();
		int cols = vector.cols();

		int indexer = 0;
		for (int i = 0; i < rows; ++i)
		{
			for (int j = 0; j < cols; ++j)
			{
				vector(i, j) = array[indexer++];
			}
		}
	}

	void ArrayToMatrixI(int* array, Eigen::MatrixXi& matrix)
	{
		int rows = matrix.rows();
		int cols = matrix.cols();

		int indexer = 0;
		for (int i = 0; i < rows; ++i)
		{
			for (int j = 0; j < cols; ++j)
			{
				matrix(i, j) = array[indexer++];
			}
		}
	}

	void ArrayToMatrix(double* array, Eigen::MatrixXd& matrix)
	{
		int rows = matrix.rows();
		int cols = matrix.cols();

		int indexer = 0;
		for (int i = 0; i < rows; ++i)
		{
			for (int j = 0; j < cols; ++j)
			{
				matrix(i, j) = array[indexer++];
			}
		}
	}

	void MatrixToArray(Eigen::MatrixXd& matrix, double* array)
	{
		int rows = matrix.rows();
		int cols = matrix.cols();

		int indexer = 0;
		for (int i = 0; i < rows; ++i)
		{
			for (int j = 0; j < cols; ++j)
			{
				array[indexer++] = matrix(i, j);
			}
		}
	}


	// Class that contains the minimmaly required data to run the ARAP solver
	class ARAPSim
	{
	private:
		Eigen::MatrixXd V, U;
		Eigen::MatrixXi F;
		Eigen::VectorXi S, b;

		igl::ARAPData arap_data;

	public:
		ARAPSim()
		{

		}

		ARAPSim(double* points, int numPoints, int* triangles, int numTris, 
			int* indicesOfConstraindedPoints, int numConstraints, int maxIter)
		{
			V.resize(numPoints, 3);
			F.resize(numTris, 3);
			b.resize(numConstraints);
			
			//Convert from arrays to Eigen Matrices/Vectors
			//Arrays are used because they are well suited for interop operations
			ArrayToMatrix(points, V);
			ArrayToMatrixI(triangles, F);
			ArrayToVectorI(indicesOfConstraindedPoints, b);

			U = V; //Set initial solution

			arap_data.max_iter = maxIter;
			arap_data.with_dynamics = true;

			igl::arap_precomputation(V, F, V.cols(), b, arap_data);
		}

		void Step(double* constrainedPositionValues, double* solution)
		{
			Eigen::MatrixXd bc(b.size(), V.cols());
			ArrayToMatrix(constrainedPositionValues, bc);

			igl::arap_solve(bc, arap_data, U);

			MatrixToArray(U, solution);
		}
	};

	

	extern "C"
	{
		__declspec(dllexport) void* __stdcall Empty(double* points, int numPoints)
		{
			Eigen::MatrixXd V;
			V.resize(numPoints, 3);
			ArrayToMatrix(points, V);
			return new ARAPSim();
		}

		__declspec(dllexport) void* __stdcall Initialize(double* points, int numPoints, int* triangles, int numTris,
			int* indicesOfConstraindedPoints, int numConstraints, int maxIter)
		{
			return new ARAPSim(points, numPoints, triangles, numTris, indicesOfConstraindedPoints, numConstraints, maxIter);
		}

		__declspec(dllexport) void __stdcall Dispose(void* handle)
		{
			ARAPSim* ptr = (ARAPSim*)handle;
			delete handle;
		}

		__declspec(dllexport) void __stdcall Step(void* handle, double* constrainedPositionValues, double* solution)
		{
			ARAPSim* ptr = (ARAPSim*)handle;
			ptr->Step(constrainedPositionValues, solution);
		}

		//Test
		__declspec(dllexport) void __stdcall Update(double* pointer, int arrayLength)
		{
			double* ptr = (double*)pointer;
			for (int i = 0; i < arrayLength; ++i)
				ptr[i] = i;
		}

		//Test
		__declspec(dllexport) int __stdcall AddOne(int value)
		{
			return value + 1;
		}
	}
}
