local CTimeout = bt.Class("CTimeout",bt.ConditionTask)
bt.CTimeout = CTimeout

function CTimeout:ctor()
    bt.ConditionTask.ctor(self)
    self.name = "CTimeout"
    self.timeout = 1
    self.currentTime = 0
end

function CTimeout:init(jsonData)
    bt.ConditionTask:init(jsonData)
    if jsonData.timeout then
        self.timeout = jsonData.timeout._value
    end
end

function CTimeout:onCheck()
    self.currentTime = self.currentTime + bt.deltaTime
    self:debug()
    if self.currentTime >= self.timeout then
        self.currentTime = 0
        return true
    else 
        return false
    end
end

function CTimeout:info()
    return string.format("If CTimeout %.2f/%.2f",self.currentTime,self.timeout)
end