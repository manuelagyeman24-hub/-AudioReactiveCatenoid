 ![wa](https://github.com/user-attachments/assets/16aa1e72-fb81-44c4-95c5-bd4aa962785f)
 import numpy as np
import matplotlib.pyplot as plt

c = 1
u = np.linspace(0, 2*np.pi, 100)
v = np.linspace(-2, 2, 100)
U, V = np.meshgrid(u, v)
X = c * np.cosh(V/c) * np.cos(U)
Y = c * np.cosh(V/c) * np.sin(U)
Z = V

fig = plt.figure(figsize=(6,6))
ax = fig.add_subplot(111, projection='3d')
ax.plot_surface(X, Y, Z, cmap='viridis', alpha=0.9)
ax.plot_wireframe(X, Y, Z, color='black', linewidth=0.5, alpha=0.3)
plt.show()


<img width="1536" height="1024" alt="image" src="https://github.com/user-attachments/assets/21c9438d-3e85-4ba4-b087-fa534fd78d1c" />
