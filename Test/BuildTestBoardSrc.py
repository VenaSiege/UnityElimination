def read_file(file_path):
    with open(file_path, 'r') as file:
        lines = file.readlines()
        return [list(map(int, line.strip().split(','))) for line in lines]

def generate_csharp_code(array):

    indent = "    "

    rows = len(array)
    cols = len(array[0])
    csharp_code = f"{indent}{indent}private static readonly int[,] PIECES = {{\n"
    
    for row in array:
        csharp_code += f"{indent}{indent}{indent}{{" + ", ".join(map(str, row)) + "},\n"
    
    return csharp_code + "        };\n"

if __name__ == "__main__":
    file_path = "./TestBoard.csv"
    array_data = read_file(file_path)
    array_data.reverse()
    array_data = list(map(list, zip(*array_data)))

    csharp_code = generate_csharp_code(array_data)
    print(csharp_code)
