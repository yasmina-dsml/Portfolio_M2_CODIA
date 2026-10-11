import torch
import torch.nn as nn

class ConvBlock(nn.Module):
    def __init__(self, c_in, c_out, k):
        super().__init__()
        self.block = nn.Sequential(
            nn.Conv2d(in_channels=c_in, out_channels=c_out, kernel_size=k),
            nn.BatchNorm2d(c_out),
            nn.ReLU(inplace=True),
            nn.MaxPool2d(kernel_size=2, stride=2)
        )
    
    def forward(self, x):
        return self.block(x)
    
    def num_params(self):
        return sum(p.numel() for p in self.parameters() if p.requires_grad)

# baseline cnn with 2 simple conv blocks (conv + BN + ReLU + MaxPool)
class BaselineConvNet(nn.Module):
    def __init__(self, n_classes):
        super().__init__()
        
        # CIFAR-10
        c_in, h, w = 3, 32, 32
        c_out1 = 8
        c_out2= 8
        k1, k2 = 5, 3

        self.block1 = ConvBlock(c_in, c_out1, k1)# bloc 1 : conv classique (5x5) + BN + ReLU + MaxPool
        self.block2 = ConvBlock(c_out1, c_out2, k2)# bloc 2 : conv classique (3x3) + BN + ReLU + MaxPool

        # calculates number of features bc the flatten layer needs to know the input size
        with torch.no_grad():
            dummy = torch.zeros(1, c_in, h, w)
            out = self.block2(self.block1(dummy))
            n_features = out.numel()

     
        self.flatten = nn.Flatten()

        
        self.clf = nn.Sequential(
            nn.Linear(n_features, 84),
            nn.ReLU(inplace=True),
            nn.Linear(84, 32),
            nn.ReLU(inplace=True),
            nn.Linear(32, n_classes)
        )
    
    
    def forward(self, x):
        x = self.block1(x)
        x = self.block2(x)
        x = self.flatten(x)
        x = self.clf(x)
        return x
    
    def backbone_num_params(self):
        return self.block1.num_params() + self.block2.num_params()

# Bottleneck "standard": 1x1 PW conv -> 3x3 Conv -> 1x1 PW conv (reduce, regular, expand)
class StandardBottleneck(nn.Module):
    def __init__(self, c_in, c_hidden, k):
        super().__init__()
        self.block = nn.Sequential(
            # 1) reduce channels
            nn.Conv2d(c_in, c_hidden, kernel_size=1, bias=False),
            nn.BatchNorm2d(c_hidden),
            nn.ReLU(inplace=True),

            # 2) regular conv
            nn.Conv2d(c_hidden, c_hidden, kernel_size=k, padding=k // 2, bias=False),
            nn.BatchNorm2d(c_hidden),
            nn.ReLU(inplace=True),

            # 3) expand back
            nn.Conv2d(c_hidden, c_in, kernel_size=1, bias=False),
            nn.BatchNorm2d(c_in),
        )

    def forward(self, x):
        return self.block(x)

    def num_params(self):
        return sum(p.numel() for p in self.parameters() if p.requires_grad)


# Bottleneck "inverted": 1x1 PW -> 3x3 DW -> 1x1 PW (expand, depthwise, reduce)
class InvertedBottleneck(nn.Module):
    def __init__(self, c_in, c_hidden, k):
        super().__init__()
        self.block = nn.Sequential(
            # 1) expand channels
            nn.Conv2d(c_in, c_hidden, kernel_size=1, bias=False),
            nn.BatchNorm2d(c_hidden),
            nn.ReLU(inplace=True),

            # 2) depthwise conv
            nn.Conv2d(
                c_hidden, c_hidden, kernel_size=k, padding=k // 2,
                groups=c_hidden, bias=False
            ),
            nn.BatchNorm2d(c_hidden),
            nn.ReLU(inplace=True),

            # 3) reduce back
            nn.Conv2d(c_hidden, c_in, kernel_size=1, bias=False),
            nn.BatchNorm2d(c_in),
        )

    def forward(self, x):
        return self.block(x)

    def num_params(self):
        return sum(p.numel() for p in self.parameters() if p.requires_grad)


class StandardConvNetBN(nn.Module):
    def __init__(self, n_classes):
        super().__init__()
        
        # CIFAR-10
        c_in, h, w = 3, 32, 32
        c_out1 = 8
        c_hidden_standard = 4  # reduce: 8 -> 4 ,expand: 4 -> 8
        k1, k2 = 5, 3

        #bloc 1 
        self.block1 = ConvBlock(c_in, c_out1, k1)

        # Bloc 2 : Standard bottleneck (reduce -> regular -> expand)
        self.block2 = StandardBottleneck(c_out1, c_hidden_standard, k2)

        # calculates number of features
        with torch.no_grad():
            dummy = torch.zeros(1, c_in, h, w)
            out = self.block2(self.block1(dummy))
            n_features = out.numel()

     
        self.flatten = nn.Flatten()

        
        self.clf = nn.Sequential(
            nn.Linear(n_features, 84),
            nn.ReLU(inplace=True),
            nn.Linear(84, 32),
            nn.ReLU(inplace=True),
            nn.Linear(32, n_classes)
        )
    
    
    def forward(self, x):
        x = self.block1(x)
        x = self.block2(x)
        x = self.flatten(x)
        x = self.clf(x)
        return x
    
    def backbone_num_params(self):
        return self.block1.num_params() + self.block2.num_params()


class InvertedConvNetBN(nn.Module):
    def __init__(self, n_classes):
        super().__init__()
        
        # CIFAR-10
        c_in, h, w = 3, 32, 32
        c_out1 = 8
        c_hidden_inverted = 16  # expand: 8 -> 16 , reduce: 16 -> 8
        k1, k2 = 5, 3

        #bloc 1 
        self.block1 = ConvBlock(c_in, c_out1, k1)

        # Bloc 2 : Inverted bottleneck (expand -> depthwise -> reduce)
        self.block2 = InvertedBottleneck(c_out1, c_hidden_inverted, k2)

        # calculates number of features
        with torch.no_grad():
            dummy = torch.zeros(1, c_in, h, w)
            out = self.block2(self.block1(dummy))
            n_features = out.numel()

     
        self.flatten = nn.Flatten()

        
        self.clf = nn.Sequential(
            nn.Linear(n_features, 84),
            nn.ReLU(inplace=True),
            nn.Linear(84, 32),
            nn.ReLU(inplace=True),
            nn.Linear(32, n_classes)
        )
    
    
    def forward(self, x):
        x = self.block1(x)
        x = self.block2(x)
        x = self.flatten(x)
        x = self.clf(x)
        return x
    
    def backbone_num_params(self):
        return self.block1.num_params() + self.block2.num_params()
